using MediatR;
using System;

namespace AsyncServices.Common.Commands
{
    public record ProcessIncoming(Guid Id, DateTime CreatedAt, object Payload) : INotification;
}
