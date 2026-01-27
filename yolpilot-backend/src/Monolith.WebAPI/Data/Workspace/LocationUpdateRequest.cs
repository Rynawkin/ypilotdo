using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;

namespace Monolith.WebAPI.Data.Workspace
{
    public class LocationUpdateRequest : BaseEntity
    {
        public int WorkspaceId { get; set; }
        public int JourneyId { get; set; }
        public int JourneyStopId { get; set; }
        public int CustomerId { get; set; }

        public decimal CurrentLatitude { get; set; }
        public decimal CurrentLongitude { get; set; }
        public string CurrentAddress { get; set; } = string.Empty;

        public decimal RequestedLatitude { get; set; }
        public decimal RequestedLongitude { get; set; }
        public string RequestedAddress { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";

        public string RequestedById { get; set; } = string.Empty;
        public string RequestedByName { get; set; } = string.Empty;
        public string? ApprovedById { get; set; }
        public string? ApprovedByName { get; set; }
        public string? RejectionReason { get; set; }

        public DateTime? ProcessedAt { get; set; }

        // Navigation
        public Workspace? Workspace { get; set; }
        public Journey? Journey { get; set; }
        public JourneyStop? JourneyStop { get; set; }
        public Customer? Customer { get; set; }
    }
}
