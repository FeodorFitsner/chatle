﻿using System;
using ChatLe.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace ChatLe
{
    public class Startup
    {
        enum DBEngine
        {
            SqlServer,
            InMemory,
            Redis,
            SQLite
        }

        readonly IHostingEnvironment _environment;
        public ILoggerFactory LoggerFactory { get; private set; }
        public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .AddJsonFile($"config.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets();
                loggerFactory.AddDebug();
            }

            builder.AddEnvironmentVariables();

            Configuration = builder.Build();

            _environment = env;
            LoggerFactory = loggerFactory;

            loggerFactory.AddConsole();
            
#if DNX451
            int io, worker;
            ThreadPool.GetMinThreads(out worker, out io);
            Console.WriteLine("Startup min worker thread {0}, min io thread {1}", worker, io);
            ThreadPool.GetMaxThreads(out worker, out io);
            Console.WriteLine("Startup max worker thread {0}, max io thread {1}", worker, io);
            ThreadPool.SetMaxThreads(32767, 1000);
            ThreadPool.SetMinThreads(50, 50);
            ThreadPool.GetMinThreads(out worker, out io);
            Console.WriteLine("Startup min worker thread {0}, min io thread {1}", worker, io);
            ThreadPool.GetMaxThreads(out worker, out io);
            Console.WriteLine("Startup max worker thread {0}, max io thread {1}", worker, io);

            var sourceSwitch = new SourceSwitch("chatle");
            loggerFactory.AddTraceSource(sourceSwitch, new ConsoleTraceListener());
#endif
        }

        public IConfiguration Configuration { get; private set; }

        public virtual void ConfigureServices(IServiceCollection services)
        {

            ConfigureEntity(services);

            services.AddCors();

            services.AddMvc();

            services.AddSignalR(options => options.Hubs.EnableDetailedErrors = _environment.EnvironmentName == "Development");

            services.AddChatLe(options => options.UserPerPage = int.Parse(Configuration["ChatConfig:UserPerPage"]));

        }

        private void ConfigureEntity(IServiceCollection services)
        {
            var dbEngine = (DBEngine)Enum.Parse(typeof(DBEngine), Configuration["DatabaseEngine"]);

            services.AddDbContext<ChatLeIdentityDbContext>(options =>
            {                
                switch (dbEngine)
                {
                    case DBEngine.InMemory:
                        options.UseInMemoryDatabase();
                        break;
                    case DBEngine.SqlServer:
                        options.UseSqlServer(Configuration["Data:DefaultConnection:ConnectionString"]);
                        break;
                    case DBEngine.SQLite:
                        options.UseSqlite(Configuration["Data:DefaultConnection:ConnectionString"]);
                        break;
                    //case DBEngine.Redis:
                    //    int port;
                    //    int database;
                    //    if (!int.TryParse(Configuration.Get("Data:Redis:Port"), out port))
                    //        port = 6379;
                    //    int.TryParse(Configuration.Get("Data:Redis:Database"), out database);
                        
                    //    //options.UseRedis(Configuration.Get("Data:Redis:Hostname"), port, database);
                    //    break;
                }
            });

            services.AddIdentity<ChatLeUser, IdentityRole>(options =>
            {
                options.SecurityStampValidationInterval = TimeSpan.FromMinutes(20);
            }).AddEntityFrameworkStores<ChatLeIdentityDbContext>();
        }

        public virtual void Configure(IApplicationBuilder app)
        {
            ConfigureErrors(app);

            app.UseCors(
                builder => builder.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials())
                .UseStaticFiles()             
                .UseWebSockets()
                .UseIdentity()
                .UseMvc(routes =>
                {
                    routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action}/{id?}",
                    defaults: new { controller = "Home", action = "Index" });
                })
                .UseSignalR()
                .UseChatLe();
        }

        protected virtual void ConfigureErrors(IApplicationBuilder app)
        {
            if (string.Equals(_environment.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
				app.UseDatabaseErrorPage();
			}
            else
            {
                // Add Error handling middleware which catches all application specific errors and
                // send the request to the following path or controller action.
                app.UseExceptionHandler("/Home/Error");
            }            
        }

		public static void Main(string[] args)
		{
			var host = new WebHostBuilder()
				.UseKestrel()
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseIISIntegration()
				.UseStartup<Startup>()
				.Build();

			host.Run();
		}
	}
}
