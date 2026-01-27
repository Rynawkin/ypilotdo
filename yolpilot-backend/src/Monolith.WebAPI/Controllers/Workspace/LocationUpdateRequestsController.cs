using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolith.WebAPI.Applications.Commands.LocationUpdateRequests;
using Monolith.WebAPI.Applications.Queries.LocationUpdateRequests;
using System.Security.Claims;

namespace Monolith.WebAPI.Controllers.Workspace
{
    [ApiController]
    [Route("api/workspace/location-update-requests")]
    [Authorize]
    public class LocationUpdateRequestsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LocationUpdateRequestsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private Guid GetAuthenticatedUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] CreateLocationUpdateRequestCommand command)
        {
            command.AuthenticatedUserId = GetAuthenticatedUserId();
            var result = await _mediator.Send(command);
            return Ok(new { requestId = result });
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var query = new GetPendingLocationUpdateRequestsQuery
            {
                AuthenticatedUserId = GetAuthenticatedUserId()
            };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        // NEW: History endpoint (Approved / Rejected)
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] string? status)
        {
            var query = new GetLocationUpdateRequestsHistoryQuery
            {
                AuthenticatedUserId = GetAuthenticatedUserId(),
                Status = status
            };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveRequest(int id, [FromBody] ApproveRequestDto dto)
        {
            var command = new ApproveLocationUpdateRequestCommand
            {
                RequestId = id,
                UpdateFutureStops = dto.UpdateFutureStops,
                AuthenticatedUserId = GetAuthenticatedUserId()
            };
            var result = await _mediator.Send(command);
            return Ok(new { success = result });
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectRequest(int id, [FromBody] RejectRequestDto dto)
        {
            var command = new RejectLocationUpdateRequestCommand
            {
                RequestId = id,
                Reason = dto.Reason,
                AuthenticatedUserId = GetAuthenticatedUserId()
            };
            var result = await _mediator.Send(command);
            return Ok(new { success = result });
        }
    }

    // DTOs
    public class ApproveRequestDto
    {
        public bool UpdateFutureStops { get; set; } = true;
    }

    public class RejectRequestDto
    {
        public string Reason { get; set; } = string.Empty;
    }
}
