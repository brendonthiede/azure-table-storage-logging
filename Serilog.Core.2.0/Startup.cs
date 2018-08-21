using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.AzureAppServices;
using Microsoft.WindowsAzure.Storage;
using Serilog;
using Serilog.Context;

namespace LoggingResearch
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            var storageAccount = CloudStorageAccount.Parse(configuration.GetSection("AzureStorageConnectionString").Value);
            var storageTableName = configuration.GetSection("LoggingStorageTableName").Value;
            Log.Logger = new LoggerConfiguration()
              .Enrich.FromLogContext()
              .MinimumLevel.Debug()
              .WriteTo.Console()
              .WriteTo.AzureTableStorage(
                storageAccount: storageAccount,
                storageTableName: storageTableName,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning,
                keyGenerator: new KeyGenerator(),
                writeInBatches: true,
                batchPostingLimit: 100,
                period: new System.TimeSpan(0, 0, 3)
                )
              .CreateLogger();
            LogContext.PushProperty("Application", "LoggingResearch");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            loggerFactory.AddSerilog();
            loggerFactory.AddAzureWebAppDiagnostics(
                new AzureAppServicesDiagnosticsSettings
                {
                    OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss zzz} [{Level}] {RequestId}-{SourceContext}: {Message}{NewLine}{Exception}"
                }
                );
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseMvc();
            appLifetime.ApplicationStopped.Register(Log.CloseAndFlush);
        }
    }
}
