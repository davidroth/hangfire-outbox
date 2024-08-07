using Fusonic.Extensions.AspNetCore.Http;
using Fusonic.Extensions.Common.Security;
using Fusonic.Extensions.Common.Transactions;
using Fusonic.Extensions.Hangfire;
using Hangfire;
using Hangfire.SqlServer;
using HangfireOutbox.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SimpleInjector;
using SimpleInjector.Lifestyles;

var builder = WebApplication.CreateBuilder(args);
var container = new Container() { Options = { DefaultScopedLifestyle = new AsyncScopedLifestyle(), DefaultLifestyle = Lifestyle.Scoped } };

var services = builder.Services;

services.AddControllers();
services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "Hangfire Outbox", Version = "v1" }));

var dataSource = builder.Configuration.GetConnectionString("app");
services.AddHangfire(x =>
{
    x.UseSerializerSettings(new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All });
    x.UseActivator(new ContainerJobActivator(container));
    x.UseSqlServerStorage(() => new Microsoft.Data.SqlClient.SqlConnection(dataSource),
       new SqlServerStorageOptions() { SchemaName = "hgf" });
});
services.AddHangfireServer();

services.AddDbContext<UserContext>(options => options.UseSqlServer(dataSource));
services.AddSimpleInjector(container, options =>
{
    options.AddAspNetCore()
           .AddControllerActivation();
});

container.Register<IUserAccessor, HttpContextUserAccessor>(Lifestyle.Singleton);
container.RegisterOutOfBandDecorators();
container.RegisterSingleton<IMediator, SimpleInjectorMediator>();

container.Register(typeof(IRequestHandler<,>), typeof(Program).Assembly);
container.Collection.Register(typeof(INotificationHandler<>), typeof(Program).Assembly);
container.RegisterSingleton<ITransactionScopeHandler, TransactionScopeHandler>();
container.RegisterDecorator(typeof(IRequestHandler<,>), typeof(TransactionalRequestHandlerDecorator<,>));
container.RegisterDecorator(typeof(INotificationHandler<>), typeof(TransactionalNotificationHandlerDecorator<>));

var app = builder.Build();
MigrateContext(app);

(app as IApplicationBuilder).UseSimpleInjector(container);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HangfireOutbox v1"));

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.UseHangfireDashboard(options: new DashboardOptions { DisplayNameFunc = DashboardHelpers.FormatJobDisplayName });

app.Run();

static void MigrateContext(WebApplication app)
{
    var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<UserContext>();
    context.Database.EnsureDeleted();
    context.Database.EnsureCreated();
}