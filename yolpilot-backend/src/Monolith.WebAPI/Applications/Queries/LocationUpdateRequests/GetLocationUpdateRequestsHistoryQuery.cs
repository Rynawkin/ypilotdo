using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.LocationUpdateRequests
{
    public class GetLocationUpdateRequestsHistoryQuery : BaseAuthenticatedCommand<List<LocationUpdateRequestHistoryDto>>
    {
        [JsonIgnore] public override bool RequiresDispatcher => true;
        public string? Status { get; set; } // "Approved" | "Rejected" | null (ikisi birden)
    }

    public class GetLocationUpdateRequestsHistoryQueryHandler
        : BaseAuthenticatedCommandHandler<GetLocationUpdateRequestsHistoryQuery, List<LocationUpdateRequestHistoryDto>>
    {
        private readonly AppDbContext _context;

        public GetLocationUpdateRequestsHistoryQueryHandler(AppDbContext context, IUserService userService)
            : base(userService)
        {
            _context = context;
        }

        protected override async Task<List<LocationUpdateRequestHistoryDto>> HandleCommand(GetLocationUpdateRequestsHistoryQuery request, CancellationToken cancellationToken)
        {
            var q = _context.LocationUpdateRequests
                .Include(lr => lr.Customer)
                .Include(lr => lr.Journey)
                .Include(lr => lr.JourneyStop)
                .Where(lr => lr.WorkspaceId == User.WorkspaceId && lr.Status != "Pending");

            // Status filtresi (Approved/Rejected)
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var s = request.Status.Trim();
                if (string.Equals(s, "Approved", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(s, "Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    q = q.Where(lr => lr.Status == s);
                }
            }

            var list = await q
                .OrderByDescending(lr => lr.ProcessedAt ?? lr.CreatedAt)
                .Select(lr => new LocationUpdateRequestHistoryDto
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
                    Status = lr.Status,

                    RequestedByName = lr.RequestedByName,
                    ApprovedByName = lr.ApprovedByName,
                    RejectionReason = lr.RejectionReason,

                    CreatedAt = lr.CreatedAt,
                    ProcessedAt = lr.ProcessedAt
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return list;
        }
    }

    public class LocationUpdateRequestHistoryDto
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
        public string Status { get; set; } = string.Empty;

        public string RequestedByName { get; set; } = string.Empty;
        public string? ApprovedByName { get; set; }
        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
