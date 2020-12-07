using System;

namespace AsyncServices.Common.Queues
{
    public record QueueMessage(Guid Id, string MessageType, EncodedData Payload);
}
