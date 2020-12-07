using System;

namespace AsyncServices.Common.Queues
{
    public class MessageReceived : EventArgs
    {
        public MessageReceived(QueueMessage message)
        {
            Message = message;
        }

        public QueueMessage Message { get; }
    }
}
