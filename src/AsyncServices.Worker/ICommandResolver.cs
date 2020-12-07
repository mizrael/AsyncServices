using MediatR;
using AsyncServices.Common.Queues;

namespace AsyncServices.Worker
{
    public interface ICommandResolver
    {
        INotification Resolve(QueueMessage message);
    }
}