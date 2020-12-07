using RabbitMQ.Client;

namespace AsyncServices.Common.Queues.RabbitMQ
{
    public interface IBusConnection
    {
        bool IsConnected { get; }

        IModel CreateChannel();
    }
}
