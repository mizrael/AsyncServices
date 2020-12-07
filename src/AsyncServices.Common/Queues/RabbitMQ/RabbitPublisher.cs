using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using AsyncServices.Common.Services;
using RabbitMQ.Client;

namespace AsyncServices.Common.Queues.RabbitMQ
{
    public class RabbitPublisher : IDisposable, IPublisher
    {
        private readonly IBusConnection _connection;
        private readonly IBasicProperties _properties;
        private readonly string _exchangeName;
        private readonly ILogger<RabbitPublisher> _logger;
        private readonly IEncoder _encoder;
        private IModel _channel;

        public RabbitPublisher(IBusConnection connection, string exchangeName, IEncoder encoder, ILogger<RabbitPublisher> logger)
        {
            if (string.IsNullOrWhiteSpace(exchangeName))
                throw new ArgumentException($"'{nameof(exchangeName)}' cannot be null or whitespace", nameof(exchangeName));

            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exchangeName = exchangeName;

            _channel = _connection.CreateChannel();
            _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Fanout);
            _properties = _channel.CreateBasicProperties();
            _properties.Persistent = true;
        }

        public Task PublishAsync(QueueMessage message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            var encodedMessage = _encoder.Encode(message);

            var policy = Policy.Handle<Exception>()
                .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, "Could not publish message '{MessageId}' to Exchange '{ExchangeName}' after {Timeout}s : {ExceptionMessage}", message.Id, _exchangeName, $"{time.TotalSeconds:n1}", ex.Message);
                });

            policy.Execute(() =>
            {
                _channel.BasicPublish(
                    exchange: _exchangeName,
                    routingKey: string.Empty,
                    mandatory: true,
                    basicProperties: _properties,
                    body: encodedMessage.Value);

                _logger.LogInformation("message '{MessageId}' with type '{MessageType}' published to Exchange '{ExchangeName}'", message.Id, message.MessageType, _exchangeName);
            });

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _channel = null;
        }
    }
}
