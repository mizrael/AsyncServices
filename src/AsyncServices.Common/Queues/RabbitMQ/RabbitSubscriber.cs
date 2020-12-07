using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AsyncServices.Common.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AsyncServices.Common.Queues.RabbitMQ
{
    public class RabbitSubscriber : ISubscriber, IDisposable
    {
        private readonly IBusConnection _connection;
        private readonly string _exchangeName;
        private readonly IDecoder _decoder;
        private readonly ILogger<RabbitSubscriber> _logger;
        private IModel _channel;
        private QueueDeclareOk _queue;

        public RabbitSubscriber(IBusConnection connection, string exchangeName, IDecoder decoder, ILogger<RabbitSubscriber> logger)
        {
            if (string.IsNullOrWhiteSpace(exchangeName))
                throw new ArgumentException($"'{nameof(exchangeName)}' cannot be null or whitespace", nameof(exchangeName));
            _exchangeName = exchangeName;

            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private void InitChannel()
        {
            _channel?.Dispose();

            _channel = _connection.CreateChannel();

            _channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Fanout);

            // since we're using a Fanout exchange, we don't specify the name of the queue
            // but we let Rabbit generate one for us. This also means that we need to store the
            // queue name to be able to consume messages from it
            _queue = _channel.QueueDeclare(queue: string.Empty,
                durable: false,
                exclusive: false,
                autoDelete: true,
                arguments: null);

            _channel.QueueBind(_queue.QueueName, _exchangeName, string.Empty, null);

            _channel.CallbackException += (sender, ea) =>
            {
                InitChannel();
                InitSubscription();
            };
        }

        private void InitSubscription()
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += OnMessageReceivedAsync;
            
            _channel.BasicConsume(queue: _queue.QueueName, autoAck: false, consumer: consumer);
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
                channel.BasicAck(eventArgs.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                var errorMsg = eventArgs.Redelivered ? "a fatal error has occurred while processing Message '{MessageId}' with type: '{MessageType}' from Exchange '{ExchangeName}' : {ExceptionMessage} . Rejecting..." :
                    "an error has occurred while processing Message '{MessageId}' with type: '{MessageType}' from Exchange '{ExchangeName}' : {ExceptionMessage} . Nacking...";

                _logger.LogWarning(ex, errorMsg, message.Id, message.MessageType, _exchangeName, ex.Message);

                //TODO: delayed renqueue, dead-lettering

                if (eventArgs.Redelivered) 
                    channel.BasicReject(eventArgs.DeliveryTag, false);
                else
                    channel.BasicNack(eventArgs.DeliveryTag, false, true);
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
