using MongoDB.Driver;

namespace AsyncServices.Common.Persistence.Mongo
{
    public interface IDbContext
    {
        IMongoCollection<ProcessedRequest> ProcessedRequests { get; }
    }
}