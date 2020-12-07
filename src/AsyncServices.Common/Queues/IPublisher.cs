using System.Threading.Tasks;

namespace AsyncServices.Common.Queues
{
    public interface IPublisher
    {
        Task PublishAsync(QueueMessage message);
    }
}