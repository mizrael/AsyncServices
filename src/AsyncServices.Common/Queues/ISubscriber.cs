using System;
using System.Threading.Tasks;

namespace AsyncServices.Common.Queues
{
    public interface ISubscriber
    {
        void Start();
        void Stop();

        event AsyncEventHandler<MessageReceived> OnMessage;
    }

    public delegate Task AsyncEventHandler<in TEvent>(object sender, TEvent @event) where TEvent : EventArgs;
}
