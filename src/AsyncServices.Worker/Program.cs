using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AsyncServices.Common.Commands;
using AsyncServices.Common.Queues;
using AsyncServices.Common.Queues.RabbitMQ;
using AsyncServices.Common.Services;
using AsyncServices.Worker.Extensions;
using RabbitMQ.Client;
using MongoDB.Driver;
using AsyncServices.Common.Persistence.Mongo;
using AsyncServices.Worker.CommandHandlers.Mongo;

namespace AsyncServices.Worker
{
    class Program
    {
        static Task Main(string[] args) =>
            CreateHostBuilder(args).Build().RunAsync();

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                var encoder = new JsonEncoder();                
                services.AddSingleton<IEncoder>(encoder);
                services.AddSingleton<IDecoder>(encoder);

                services.AddSingleton<IConnectionFactory>(ctx =>
                {
                    var config = ctx.GetRequiredService<IConfiguration>();
                    var rabbitConfig = config.GetSection("RabbitMQ");
                    var connectionFactory = new ConnectionFactory()
                    {
                        HostName = rabbitConfig["HostName"],
                        UserName = rabbitConfig["UserName"],
                        Password = rabbitConfig["Password"],
                        Port = AmqpTcpEndpoint.UseDefaultPort,
                        DispatchConsumersAsync = true
                    };
                    return connectionFactory;
                });

                services.AddSingleton<IBusConnection, RabbitPersistentConnection>();
                services.AddSingleton<ICommandResolver>(ctx =>
                {
                    var decoder = ctx.GetRequiredService<IDecoder>();
                    var assemblies = new[]
                    {
                        typeof(ProcessIncoming).Assembly
                    };
                    return new CommandResolver(decoder, assemblies);
                });

                services.AddSingleton<ISubscriber>(ctx =>
                {
                    var connection = ctx.GetRequiredService<IBusConnection>();
                    var decoder = ctx.GetRequiredService<IDecoder>();
                    var config = ctx.GetRequiredService<IConfiguration>();
                    var logger = ctx.GetRequiredService<ILogger<RabbitSubscriber>>();
                    var rabbitConfig = config.GetSection("RabbitMQ");

                    var exchangeName = rabbitConfig["Exchange"];

                    return new RabbitSubscriber(connection, exchangeName, decoder, logger);
                });

                services.AddSingleton(ctx =>
                {
                    var config = ctx.GetRequiredService<IConfiguration>();

                    var mongoCfg = config.GetSection("Mongo");
                    var connStr = mongoCfg["ConnectionString"];
                    return new MongoClient(connectionString: connStr);
                })
                .AddSingleton(ctx =>
                {
                    var config = ctx.GetRequiredService<IConfiguration>();

                    var mongoCfg = config.GetSection("Mongo");
                    var dbName = mongoCfg["DbName"];
                    var client = ctx.GetRequiredService<MongoClient>();
                    var database = client.GetDatabase(dbName);
                    return database;
                }).AddSingleton<IDbContext, DbContext>();

                services.AddMediatR(new[]{
                    typeof(ProcessIncoming),
                    typeof(ProcessIncomingHandler)
                });

                services.AddHostedService<MessagesBackgroundService>();
            });
    }
}
