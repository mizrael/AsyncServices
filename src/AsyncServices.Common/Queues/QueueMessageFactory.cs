using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AsyncServices.Common.Services;

namespace AsyncServices.Common.Queues
{
    public class QueueMessageFactory : IQueueMessageFactory
    {
        private readonly IEncoder _encoder;
        private readonly IDictionary<Type, object> _messageIdGenerators;

        public QueueMessageFactory(IEncoder encoder)
        {
            _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
            _messageIdGenerators = new Dictionary<Type, object>();
        }

        public Task<QueueMessage> CreateAsync<T>(T data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            var type = typeof(T);
            var payload = _encoder.Encode(data);
            var messageId = GenerateMessageId(data);
            var message = new QueueMessage(messageId, type.FullName, payload);
            return Task.FromResult(message);
        }

        private Guid GenerateMessageId<T>(T data)
        {
            var dataType = typeof(T);

            if(!_messageIdGenerators.TryGetValue(dataType, out var tmp) || tmp is not Func<T, Guid> generator) 
                return Guid.NewGuid();

            return generator(data);
        }

        public void RegisterMessageIdGenerator<T>(Func<T, Guid> generator){
            if (generator is null)            
                throw new ArgumentNullException(nameof(generator));

            var dataType = typeof(T);

            if(!_messageIdGenerators.ContainsKey(dataType))
                _messageIdGenerators.Add(dataType, generator);
            else
                _messageIdGenerators[dataType] = generator;
        }
    }
}
