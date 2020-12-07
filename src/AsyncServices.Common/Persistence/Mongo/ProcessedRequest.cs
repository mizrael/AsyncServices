using MongoDB.Bson;
using System;

namespace AsyncServices.Common.Persistence.Mongo
{
    public record ProcessedRequest(Guid Id, DateTime CreatedAt, DateTime ProcessedAt, DateTime CompletedAt, BsonDocument Data);
}
