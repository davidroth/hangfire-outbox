using Fusonic.Extensions.AspNetCore.Http;
using Fusonic.Extensions.Common.Security;
using Fusonic.Extensions.Common.Transactions;
using Fusonic.Extensions.Hangfire;
using Fusonic.Extensions.MediatR;
using Hangfire;
using Hangfire.SqlServer;
using HangfireCqrsOutbox.Data;
using MediatR;
using MediatR.Pipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using SimpleInjector;
using SimpleInjector.Lifestyles;

var builder = WebApplication.CreateBuilder(args);
var Container = new Container() { Options = { DefaultScopedLifestyle = new AsyncScopedLifestyle(), DefaultLifestyle = Lifestyle.Scoped } };

const string dataSource = "Data Source=(localdb)\\mssqllocaldb; Initial Catalog=hangfire-outbox";

var services = builder.Services;

services.AddControllers();
services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "hangfire_outbox", Version = "v1" }));

services.AddHangfire(x =>
{
    x.UseSerializerSettings(new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All });
    x.UseActivator(new ContainerJobActivator(Container));
    x.UseSqlServerStorage(() => new Microsoft.Data.SqlClient.SqlConnection(dataSource),
       new SqlServerStorageOptions() { SchemaName = "hgf" });
});
services.AddHangfireServer();

services.AddDbContext<UserContext>(options => options.UseSqlServer(dataSource));
services.AddSimpleInjector(Container, options =>
{
    options.AddAspNetCore()
           .AddControllerActivation();
});

Container.Register<IUserAccessor, HttpContextUserAccessor>(Lifestyle.Singleton);
Container.RegisterOutOfBandDecorators();
Container.RegisterSingleton<IMediator, Mediator>();
Container.RegisterInstance(new ServiceFactory(Container.GetInstance));

Container.Register(typeof(IRequestHandler<,>), typeof(Program).Assembly);
Container.Collection.Register(typeof(INotificationHandler<>), typeof(Program).Assembly);
Container.RegisterSingleton<ITransactionScopeHandler, TransactionScopeHandler>();
Container.RegisterDecorator(typeof(IRequestHandler<,>), typeof(TransactionalRequestHandlerDecorator<,>));
Container.RegisterDecorator(typeof(INotificationHandler<>), typeof(TransactionalNotificationHandlerDecorator<>));

Container.Collection.Register(typeof(IPipelineBehavior<,>), new[]
{
                    typeof(RequestPreProcessorBehavior<,>),
                    typeof(RequestPostProcessorBehavior<,>)
                });

Container.Collection.Register(typeof(IRequestPreProcessor<>), typeof(Program).Assembly);
Container.Collection.Register(typeof(IRequestPostProcessor<,>), typeof(Program).Assembly);

var app = builder.Build();
MigrateContext(app);

(app as IApplicationBuilder).UseSimpleInjector(Container);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "hangfire_outbox v1"));

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.UseEndpoints(endpoints => endpoints.MapControllers());
app.UseHangfireDashboard(options: new DashboardOptions { DisplayNameFunc = DashboardHelpers.FormatJobDisplayName });

app.Run();

static void MigrateContext(WebApplication app)
{
    var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<UserContext>();
    context.Database.EnsureCreated();
}