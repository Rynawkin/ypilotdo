using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolith.WebAPI.Applications.Commands.Customers;
using Monolith.WebAPI.Applications.Queries.Customers;
using Monolith.WebAPI.Responses.Workspace;
using System.Security.Claims;

namespace Monolith.WebAPI.Controllers.Workspace
{
    [ApiController]
    [Route("api/workspace/customers")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CustomersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // Helper method to get authenticated user ID
        private Guid GetAuthenticatedUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }

        // GET: api/workspace/customers
        [HttpGet]
        public async Task<ActionResult<List<CustomerResponse>>> GetCustomers(
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? priority = null,
            [FromQuery] int? pageNumber = null,
            [FromQuery] int? pageSize = null)
        {
            var query = new GetCustomersQuery
            {
                AuthenticatedUserId = GetAuthenticatedUserId(),
                SearchTerm = searchTerm,
                Priority = priority,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        // GET: api/workspace/customers/search?query=xxx
        [HttpGet("search")]
        public async Task<ActionResult<List<CustomerResponse>>> SearchCustomers([FromQuery] string query)
        {
            var searchQuery = new SearchCustomersQuery 
            { 
                SearchTerm = query,
                AuthenticatedUserId = GetAuthenticatedUserId()
            };
            var result = await _mediator.Send(searchQuery);
            return Ok(result);
        }

        // GET: api/workspace/customers/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerResponse>> GetCustomer(int id)
        {
            var query = new GetCustomerQuery 
            { 
                Id = id,
                AuthenticatedUserId = GetAuthenticatedUserId()
            };
            var result = await _mediator.Send(query);
            
            if (result == null)
            {
                return NotFound(new { message = "Müşteri bulunamadı" });
            }
            
            return Ok(result);
        }

        // POST: api/workspace/customers
        [HttpPost]
        public async Task<ActionResult<CustomerResponse>> CreateCustomer([FromBody] CreateCustomerCommand command)
        {
            command.AuthenticatedUserId = GetAuthenticatedUserId();
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetCustomer), new { id = result.Id }, result);
        }

        // PUT: api/workspace/customers/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<CustomerResponse>> UpdateCustomer(int id, [FromBody] UpdateCustomerCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest(new { message = "ID uyuşmazlığı" });
            }

            command.AuthenticatedUserId = GetAuthenticatedUserId();
            var result = await _mediator.Send(command);
            
            if (result == null)
            {
                return NotFound(new { message = "Müşteri bulunamadı" });
            }
            
            return Ok(result);
        }

        // DELETE: api/workspace/customers/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCustomer(int id)
        {
            var command = new DeleteCustomerCommand 
            { 
                Id = id,
                AuthenticatedUserId = GetAuthenticatedUserId()
            };
            var result = await _mediator.Send(command);
            
            if (!result)
            {
                return NotFound(new { message = "Müşteri bulunamadı" });
            }
            
            return NoContent();
        }

        // POST: api/workspace/customers/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult<BulkImportResponse>> BulkImportCustomers([FromBody] BulkImportCustomersCommand command)
        {
            command.AuthenticatedUserId = GetAuthenticatedUserId();
            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }
}
