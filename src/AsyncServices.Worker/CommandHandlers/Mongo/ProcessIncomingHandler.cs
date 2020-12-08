using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AsyncServices.Common.Commands;
using AsyncServices.Common.Persistence.Mongo;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;

namespace AsyncServices.Worker.CommandHandlers.Mongo
{
    public class ProcessIncomingHandler : MediatR.INotificationHandler<ProcessIncoming>
    {
        private readonly IDbContext _dbContext;
        private readonly ILogger<ProcessIncomingHandler> _logger;

        private static long _run = 0;

        public ProcessIncomingHandler(ILogger<ProcessIncomingHandler> logger, IDbContext dbContext)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task Handle(ProcessIncoming command, CancellationToken cancellationToken)
        {
            _run++;

            if (command is null)
                throw new ArgumentNullException(nameof(command));

            _logger.LogInformation("{HandlerExecutionCount} - processing request '{MessageId}' ...", _run, command.Id);

            var processedAt = DateTime.UtcNow;

            // just pretend we're doing something...
            await Task.Delay(2000);            
            if ((_run & 1) == 0){
                _logger.LogError("{HandlerExecutionCount} - failed '{MessageId}' whooops", _run, command.Id);
                throw new Exception($"{_run} whooops!");            
            }                

            BsonDocument payload = null;
            if(command.Payload is not null)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(command.Payload);
                payload = BsonDocument.Parse(json);
            }
            
            var model = new ProcessedRequest(command.Id, command.CreatedAt, processedAt, DateTime.UtcNow, payload);
            await _dbContext.ProcessedRequests.InsertOneAsync(model, options: null, cancellationToken);

            _logger.LogInformation("{HandlerExecutionCount} - request '{MessageId}' processed!", _run, command.Id);
        }
    }
}
