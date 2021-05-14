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
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace HangfireCqrsOutbox
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;
        public IConfiguration Configuration { get; }
        public Container Container { get; } = new Container() { Options = { DefaultScopedLifestyle = new AsyncScopedLifestyle(), DefaultLifestyle = Lifestyle.Scoped } };

        public void ConfigureServices(IServiceCollection services)
        {
            const string dataSource = "Data Source=(localdb)\\mssqllocaldb; Initial Catalog=hangfire-outbox";

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

            Container.Register(typeof(IRequestHandler<,>), GetType().Assembly);
            Container.Collection.Register(typeof(INotificationHandler<>), GetType().Assembly);
            Container.RegisterSingleton<ITransactionScopeHandler, TransactionScopeHandler>();
            Container.RegisterDecorator(typeof(IRequestHandler<,>), typeof(TransactionalRequestHandlerDecorator<,>));
            Container.RegisterDecorator(typeof(INotificationHandler<>), typeof(TransactionalNotificationHandlerDecorator<>));

            Container.Collection.Register(typeof(IPipelineBehavior<,>), new[]
            {
                    typeof(RequestPreProcessorBehavior<,>),
                    typeof(RequestPostProcessorBehavior<,>)
                });

            Container.Collection.Register(typeof(IRequestPreProcessor<>), GetType().Assembly);
            Container.Collection.Register(typeof(IRequestPostProcessor<,>), GetType().Assembly);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            MigrateContext(app);

            app.UseSimpleInjector(Container);
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "hangfire_outbox v1"));
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
            app.UseHangfireDashboard(options: new DashboardOptions { DisplayNameFunc = DashboardHelpers.FormatJobDisplayName });
            app.UseHangfireServer();
        }

        private static void MigrateContext(IApplicationBuilder app)
        {
            var scope = app.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<UserContext>();
            context.Database.EnsureCreated();
        }
    }
}