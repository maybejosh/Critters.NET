﻿using System.Data;
using System.Data.Common;
using CritterServer.Domains;
using CritterServer.DataAccess;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Sinks.File;
using CritterServer.Domains.Components;
using Serilog.Events;

namespace CritterServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance);
            services.AddScoped<IDbConnection>((sp) =>
            {
                var conn = DbProviderFactories.GetFactory("Npgsql").CreateConnection();
                conn.ConnectionString = "Server=localhost; Port=5432; User Id=LocalApp;Password=localapplicationpassword;Database=CrittersDB";
                return conn;
            });

            configureLogging();

            //domains
            services.AddTransient<UserAuthenticationDomain>();

            //repositories
            services.AddTransient<IUserRepository, UserRepository>();

            //components
            services.AddJwt(Configuration);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseAuthentication();
            app.UseMvc();
        }

        private void configureLogging()
        {
            var stringLevel = Configuration.GetSection("Logging:LogLevel:Default").Value;

            LogEventLevel logLevel;
            switch (stringLevel)
            {
                case "Verbose": logLevel = LogEventLevel.Verbose; break;
                case "Debug": logLevel = LogEventLevel.Debug; break;
                case "Information": logLevel = LogEventLevel.Information; break;
                case "Warning": logLevel = LogEventLevel.Warning; break;
                case "Error": logLevel = LogEventLevel.Error; break;
                case "Fatal": logLevel = LogEventLevel.Fatal; break;
                default: logLevel = LogEventLevel.Warning; break;
            }

            Log.Logger = new LoggerConfiguration()
               .Enrich.FromLogContext()
               .WriteTo.EventLog("Critters.NET", "Critters.NET", "343GuiltySpark")
               .WriteTo.File(path: "bin/logs/Critter.log", rollingInterval: RollingInterval.Day,
               fileSizeLimitBytes: 1000 * 1000 * 100, //100mb
               rollOnFileSizeLimit: true)
               .WriteTo.Debug()
               .MinimumLevel.Is(logLevel)
                   .CreateLogger();

            Log.Warning("Logger configured to {debugLevel}", stringLevel);
        }
    }
}