using System;
using MediatR;

namespace AsyncServices.API.Queries
{
    public record ProcessedRequestById(Guid Id) : IRequest<ProcessedRequest>;

    public record ProcessedRequest(Guid Id, DateTime CreatedAt, DateTime ProcessedAt, DateTime CompletedAt, object Data);
}
