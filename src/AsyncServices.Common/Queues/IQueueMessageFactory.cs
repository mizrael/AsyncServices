using System.Threading.Tasks;

namespace AsyncServices.Common.Queues
{
    public interface IQueueMessageFactory
    {
        Task<QueueMessage> CreateAsync<T>(T data);
    }
}