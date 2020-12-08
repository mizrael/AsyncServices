using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using AsyncServices.Common.Queues;
using AsyncServices.Common.Queues.RabbitMQ;
using AsyncServices.Common.Services;
using RabbitMQ.Client;
using Serilog;
using MongoDB.Driver;
using AsyncServices.Common.Persistence.Mongo;
using AsyncServices.Worker.Extensions;
using System.Collections.Generic;
using System;
using AsyncServices.Common.Commands;

namespace AsyncServices.API
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
            ConfigureLogging(services);

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "AsyncProcessor.Web.API", Version = "v1" });
            });

            var encoder = new JsonEncoder();
            services.AddSingleton<IEncoder>(encoder);
            services.AddSingleton<IDecoder>(encoder);

            services.AddSingleton<IQueueMessageFactory>(ctx =>
            {
                var factory = new QueueMessageFactory(ctx.GetRequiredService<IEncoder>());
                factory.RegisterMessageIdGenerator<ProcessIncoming>(p => p.Id);
                return factory;
            });

            services.AddSingleton<IConnectionFactory>(ctx =>
            {
                var rabbitConfig = this.Configuration.GetSection("RabbitMQ");
                var connectionFactory = new ConnectionFactory()
                {
                    HostName = rabbitConfig["HostName"],
                    UserName = rabbitConfig["UserName"],
                    Password = rabbitConfig["Password"],
                    Port = AmqpTcpEndpoint.UseDefaultPort
                };
                return connectionFactory;
            });

            services.AddSingleton<IBusConnection, RabbitPersistentConnection>();

            services.AddScoped<IPublisher>(ctx =>
            {
                var connection = ctx.GetRequiredService<IBusConnection>();
                var encoder = ctx.GetRequiredService<IEncoder>();
                var logger = ctx.GetRequiredService<ILogger<RabbitPublisher>>();

                var rabbitConfig = this.Configuration.GetSection("RabbitMQ");
                var exchangeName = rabbitConfig["Exchange"];

                return new RabbitPublisher(connection, exchangeName, encoder, logger);
            });

            services.AddSingleton(ctx =>
            {
                var mongoCfg = Configuration.GetSection("Mongo");
                var connStr = mongoCfg["ConnectionString"];
                return new MongoClient(connectionString: connStr);
            })
            .AddSingleton(ctx =>
            {
                var mongoCfg = Configuration.GetSection("Mongo");
                var dbName = mongoCfg["DbName"];
                var client = ctx.GetRequiredService<MongoClient>();
                var database = client.GetDatabase(dbName);
                return database;
            }).AddSingleton<IDbContext, DbContext>();

            services.AddMediatR(typeof(Queries.ProcessedRequestById));

            services.AddHealthChecks() //TODO: ensure connections are not spawned more than necessary
                .AddRabbitMQ(connectionFactory: (ctx) => ctx.GetRequiredService<IConnectionFactory>().CreateConnection(), name: "rabbit", tags: new[] { "infrastructure" });
        }

        private void ConfigureLogging(IServiceCollection services)
        {
            var appInsightsConfig = TelemetryConfiguration.CreateDefault();
            var logger = new Serilog.LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.ApplicationInsights(appInsightsConfig, TelemetryConverter.Traces)
                .CreateLogger();
            services.AddLogging(lb => lb.AddSerilog(logger));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (!env.IsProduction())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AsyncProcessor.Web.API v1"));
            }
            else
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                endpoints.MapControllers();
            });

            app.UseWelcomePage();
        }
    }
}
