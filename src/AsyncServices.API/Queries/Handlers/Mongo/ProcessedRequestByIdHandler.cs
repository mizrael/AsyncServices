using AsyncServices.Common.Persistence.Mongo;
using MediatR;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncServices.API.Queries.Handlers.Mongo
{
    public class ProcessedRequestByIdHandler : IRequestHandler<ProcessedRequestById, ProcessedRequest>
    {
        private readonly IDbContext _dbContext;

        public ProcessedRequestByIdHandler(IDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<ProcessedRequest> Handle(ProcessedRequestById request, CancellationToken cancellationToken)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            var cursor = await _dbContext.ProcessedRequests.FindAsync(c => c.Id == request.Id,
                null, cancellationToken);
            var entity = await cursor.FirstOrDefaultAsync(cancellationToken);
            if (entity is null)
                return null;

            var payload = BsonSerializer.Deserialize<ExpandoObject>(entity.Data);            
            return new ProcessedRequest(entity.Id, entity.CreatedAt, entity.ProcessedAt, entity.CompletedAt, payload);
        }
    }
}
