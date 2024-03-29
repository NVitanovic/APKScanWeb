﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using APKScanWeb.Models;
using Microsoft.Extensions.Options;
using APKScanWeb.Middleware;

namespace APKScanWeb
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("configuration.json", false, true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //Allow access to the API trough any origin
            services.AddCors(options =>
            {
                options.AddPolicy("AnyOrigin",
                    builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });

            // Add framework services.
            services.AddMvc();

            //load configuration
            var cfg = Configuration.GetSection("Configuration");
            services.Configure<Configuration>(cfg);
            services.AddSingleton<IConfiguration>(Configuration);

            //make configuration global
            Program.config = ConfigurationExtensions.Get<Models.Configuration>(Configuration, "Configuration");

            //run the Redis bacground worker
            BackgrounRedisWorker bw = BackgrounRedisWorker.getInstance();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            app.UseCors("AnyOrigin");
            //app.UseMiddleware<RealIP>();
            app.UseMiddleware<APILimit>();
            app.UseMvc();
            
        }
    }
    //make configuration great again
    public static class ConfigurationExtensions
    {
        public static T Get<T>(this IConfiguration config, string key) where T : new()
        {
            var instance = new T();
            config.GetSection(key).Bind(instance);
            return instance;
        }
    }
}
