using Elsa;
using Elsa.Extensions;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.MySql;
using Elsa.Persistence.EntityFramework.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace WorkflowServer
{
    public class Startup
    {
        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        private IWebHostEnvironment Environment { get; }
        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var elsaConnStr = "server=localhost;port=3306;database=workflow;Uid=root;Pwd=!mypassword";
            var quartzConnStr = "server=localhost;port=3306;database=quartz;Uid=root;Pwd=!mypassword";
            var sqlServer = "Data Source=localhost;Initial Catalog=elsa;User ID=sa;Password=!mypassword;";

            //services.AddRedis("localhost:6379"); //Register singleton IConnectionMultiplexer

            var elsaSection = Configuration.GetSection("Elsa");
            
            // Elsa services.
            services
                .AddElsa(elsa => elsa
                    .UseEntityFrameworkPersistence(ef => ef.UseMySql(elsaConnStr), autoRunMigrations: true)
                    //.UseEntityFrameworkPersistence(ef => ef.UseSqlServer(sqlServer), autoRunMigrations: true)
                    //.UseEntityFrameworkPersistence(ef => ef.UseSqlite("Data Source=sqlite.db;"), autoRunMigrations: true)
                    .AddConsoleActivities()
                    .AddHttpActivities(elsaSection.GetSection("Server").Bind)
                    //.AddQuartzTemporalActivities(
                    //    configureQuartz: quartz => quartz
                    //        .UsePersistentStore(store =>
                    //            {
                    //                store.UseProperties = true;
                    //                store.UseMySql(quartzConnStr);
                    //                store.UseClustering();
                    //                store.UseJsonSerializer();
                    //            })
                    //    )
                    .AddWorkflowsFrom<Startup>()
                    .AddWorkflow<HelloHttpWorkflow>()
                    //.UseRedisCacheSignal() //Distributed Cache Signal Provider
                    //.ConfigureDistributedLockProvider(opt => opt.UseRedisLockProvider()) //Distributed Lock Provider
                );

            // Elsa API endpoints.
            services.AddElsaApiEndpoints();

            // For Dashboard.
            services.AddRazorPages();

        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app
                .UseStaticFiles() // For Dashboard.
                .UseHttpActivities()
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    // Elsa API Endpoints are implemented as regular ASP.NET Core API controllers.
                    endpoints.MapControllers();

                    // For Dashboard.
                    endpoints.MapFallbackToPage("/_Host");
                });
        }
    }
}