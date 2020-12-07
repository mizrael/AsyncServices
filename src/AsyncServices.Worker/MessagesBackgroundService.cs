using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AsyncServices.Common.Queues;

namespace AsyncServices.Worker
{
    public class MessagesBackgroundService : BackgroundService
    {
        private readonly ISubscriber _subscriber;
        private readonly ICommandResolver _commandResolver;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MessagesBackgroundService> _logger;
        private readonly IHostEnvironment _env;

        public MessagesBackgroundService(ISubscriber subscriber, ICommandResolver commandResolver,
            IServiceScopeFactory scopeFactory,
            IHostEnvironment env,
            ILogger<MessagesBackgroundService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
            _commandResolver = commandResolver ?? throw new ArgumentNullException(nameof(commandResolver));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"starting Message Worker on {_env.EnvironmentName} ...");

            _subscriber.OnMessage += OnMessageReceived;
            _subscriber.Start();

            _logger.LogInformation("Message Worker started, consuming messages...");

            return base.StartAsync(cancellationToken);
        }

        private async Task OnMessageReceived(object sender, MessageReceived @event)
        {
            _logger.LogInformation("processing Message '{MessageId}' with type: '{MessageType}' ...", @event.Message.Id, @event.Message.MessageType);

            var command = _commandResolver.Resolve(@event.Message);
            using var innerScope = _scopeFactory.CreateScope();
            var mediatr = innerScope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
            await mediatr.Publish(command);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("stopping Message Worker...");

            await base.StopAsync(cancellationToken);

            _subscriber.OnMessage -= OnMessageReceived;
            _subscriber.Stop();

            _logger.LogInformation("stopping Message Worker");
        }
    }
}
