using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AsyncServices.Common.Commands;
using AsyncServices.Common.Queues;
using AsyncServices.API.Queries;

namespace AsyncServices.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RequestsController : ControllerBase
    {
        private readonly IPublisher _publisher;
        private readonly IQueueMessageFactory _queueMessageFactory;
        private readonly MediatR.IMediator _mediator;

        public RequestsController(IPublisher publisher, IQueueMessageFactory queueMessageFactory, MediatR.IMediator mediator)
        {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _queueMessageFactory = queueMessageFactory ?? throw new ArgumentNullException(nameof(queueMessageFactory));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [HttpGet]
        [ActionName(nameof(GetDetails))]
        [Route("{id:guid}")]
        public async Task<IActionResult> GetDetails(Guid id)
        {
            var query = new ProcessedRequestById(id);
            var result = await _mediator.Send(query);
            if (result is null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> BeginProcessing(dynamic request)
        {
            if (request is null)
                return BadRequest();
            
            var command = new ProcessIncoming(Guid.NewGuid(), DateTime.UtcNow, request);                                   

            var message = await _queueMessageFactory.CreateAsync(command);
            await _publisher.PublishAsync(message);

            var body = new
            {
                id = command.Id
            };
            return AcceptedAtAction(nameof(GetDetails), routeValues: body, value: body);
        }
    }
}
