using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AsyncServices.Common.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Generic;

namespace AsyncServices.Common.Queues.RabbitMQ
{
    public record RabbitSubscriberOptions(string ExchangeName, string QueueName, string DeadLetterExchangeName, string DeadLetterQueue);

    public class RabbitSubscriber : ISubscriber, IDisposable
    {
        private readonly IBusConnection _connection;
        private readonly IDecoder _decoder;
        private readonly ILogger<RabbitSubscriber> _logger;
        private readonly RabbitSubscriberOptions _options;
        private IModel _channel;

        public RabbitSubscriber(IBusConnection connection, RabbitSubscriberOptions options, IDecoder decoder, ILogger<RabbitSubscriber> logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private void InitChannel()
        {
            _channel?.Dispose();

            _channel = _connection.CreateChannel();

            _channel.ExchangeDeclare(exchange: _options.DeadLetterExchangeName, type: ExchangeType.Fanout);
            _channel.QueueDeclare(queue: _options.DeadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            _channel.QueueBind(_options.DeadLetterQueue, _options.DeadLetterExchangeName, routingKey: string.Empty, arguments: null);

            _channel.ExchangeDeclare(exchange: _options.ExchangeName, type: ExchangeType.Fanout);
            _channel.QueueDeclare(queue: _options.QueueName,
                durable: false,
                exclusive: false,
                autoDelete: true,
                arguments: new Dictionary<string, object>()
                    {
                        {"x-dead-letter-exchange", _options.DeadLetterExchangeName},
                        {"x-dead-letter-routing-key", _options.ExchangeName}
                    });
            _channel.QueueBind(_options.QueueName, _options.ExchangeName, string.Empty, null);

            _channel.CallbackException += (sender, ea) =>
            {
                _logger.LogError(ea.Exception, "the RabbitMQ Channel has encountered an error: {ExceptionMessage}", ea.Exception.Message);

                InitChannel();
                InitSubscription();
            };
        }

        private void InitSubscription()
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += OnMessageReceivedAsync;

            _channel.BasicConsume(queue: _options.QueueName, autoAck: false, consumer: consumer);
        }

        private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
        {
            var consumer = sender as IBasicConsumer;
            var channel = consumer?.Model ?? _channel;

            QueueMessage message = null;

            try
            {
                message = _decoder.Decode<QueueMessage>(eventArgs.Body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "an exception has occured while decoding queue message from Exchange '{ExchangeName}', message cannot be processed. Error: {ExceptionMessage}",
                                 eventArgs.Exchange, ex.Message);
                return;
            }

            try
            {
                await this.OnMessage(this, new MessageReceived(message));
                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                var errorMsg = eventArgs.Redelivered ? "a fatal error has occurred while processing Message '{MessageId}' with type: '{MessageType}' from Exchange '{ExchangeName}' : {ExceptionMessage} . Rejecting..." :
                    "an error has occurred while processing Message '{MessageId}' with type: '{MessageType}' from Exchange '{ExchangeName}' : {ExceptionMessage} . Nacking...";

                _logger.LogWarning(ex, errorMsg, message.Id, message.MessageType, _options.ExchangeName, ex.Message);

                if (eventArgs.Redelivered)
                    channel.BasicReject(eventArgs.DeliveryTag, requeue: false);
                else
                    channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
            }
        }

        public event AsyncEventHandler<MessageReceived> OnMessage;

        public void Start()
        {
            InitChannel();
            InitSubscription();
        }

        public void Stop()
        {
            if (null != _channel)
            {
                if (_channel.IsOpen)
                    _channel.Close();

                _channel.Dispose();
                _channel = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
