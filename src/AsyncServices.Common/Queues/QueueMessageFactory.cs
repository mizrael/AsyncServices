using System;
using System.Threading.Tasks;
using AsyncServices.Common.Services;

namespace AsyncServices.Common.Queues
{
    public class QueueMessageFactory : IQueueMessageFactory
    {
        private readonly IEncoder _encoder;

        public QueueMessageFactory(IEncoder encoder)
        {
            _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
        }

        public Task<QueueMessage> CreateAsync<T>(T data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            var type = typeof(T);
            var payload = _encoder.Encode(data);
            var message = new QueueMessage(Guid.NewGuid(), type.FullName, payload);
            return Task.FromResult(message);
        }
    }
}
