using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System;

namespace AsyncServices.Common.Persistence.Mongo
{
    public class DbContext : IDbContext
    {
        private readonly IMongoDatabase _db;

        private static readonly IBsonSerializer guidSerializer = new GuidSerializer(GuidRepresentation.Standard);

        public DbContext(IMongoDatabase db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));

            ProcessedRequests = _db.GetCollection<ProcessedRequest>("processedRequests");
        }

        static DbContext()
        {
            BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;

            if (!BsonClassMap.IsClassMapRegistered(typeof(ProcessedRequest)))
                BsonClassMap.RegisterClassMap<ProcessedRequest>(mapper =>
                {
                    mapper.MapIdField(c => c.Id).SetSerializer(guidSerializer);
                    mapper.MapProperty(c => c.CreatedAt);
                    mapper.MapProperty(c => c.ProcessedAt);
                    mapper.MapProperty(c => c.CompletedAt);
                    mapper.MapProperty(c => c.Data);
                    mapper.MapCreator(c => new ProcessedRequest(c.Id, c.CreatedAt, c.ProcessedAt, c.CompletedAt, c.Data));
                });
        }
     
        public IMongoCollection<ProcessedRequest> ProcessedRequests { get; }
    }
}
