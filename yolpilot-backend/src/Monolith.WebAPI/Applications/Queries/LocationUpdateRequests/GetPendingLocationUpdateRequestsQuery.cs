using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.LocationUpdateRequests
{
    public class GetPendingLocationUpdateRequestsQuery : BaseAuthenticatedCommand<List<LocationUpdateRequestDto>>
    {
        [JsonIgnore] public override bool RequiresDispatcher => true;
    }

    public class GetPendingLocationUpdateRequestsQueryHandler
        : BaseAuthenticatedCommandHandler<GetPendingLocationUpdateRequestsQuery, List<LocationUpdateRequestDto>>
    {
        private readonly AppDbContext _context;

        public GetPendingLocationUpdateRequestsQueryHandler(AppDbContext context, IUserService userService)
            : base(userService)
        {
            _context = context;
        }

        protected override async Task<List<LocationUpdateRequestDto>> HandleCommand(GetPendingLocationUpdateRequestsQuery request, CancellationToken cancellationToken)
        {
            var requests = await _context.LocationUpdateRequests
                .Include(lr => lr.Customer)
                .Include(lr => lr.Journey)
                .Include(lr => lr.JourneyStop)
                .Where(lr => lr.WorkspaceId == User.WorkspaceId && lr.Status == "Pending")
                .OrderByDescending(lr => lr.CreatedAt)
                .Select(lr => new LocationUpdateRequestDto
                {
                    Id = lr.Id,
                    JourneyId = lr.JourneyId,
                    JourneyName = lr.Journey != null ? (lr.Journey.Name ?? $"Journey #{lr.JourneyId}") : "",
                    CustomerId = lr.CustomerId,
                    CustomerName = lr.Customer != null ? lr.Customer.Name : "",
                    CurrentLatitude = lr.CurrentLatitude,
                    CurrentLongitude = lr.CurrentLongitude,
                    CurrentAddress = lr.CurrentAddress,
                    RequestedLatitude = lr.RequestedLatitude,
                    RequestedLongitude = lr.RequestedLongitude,
                    RequestedAddress = lr.RequestedAddress,
                    Reason = lr.Reason,
                    RequestedByName = lr.RequestedByName,
                    CreatedAt = lr.CreatedAt
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return requests;
        }
    }

    public class LocationUpdateRequestDto
    {
        public int Id { get; set; }
        public int JourneyId { get; set; }
        public string JourneyName { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        public decimal CurrentLatitude { get; set; }
        public decimal CurrentLongitude { get; set; }
        public string CurrentAddress { get; set; } = string.Empty;

        public decimal RequestedLatitude { get; set; }
        public decimal RequestedLongitude { get; set; }
        public string RequestedAddress { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;
        public string RequestedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
