using System;
using System.ComponentModel.DataAnnotations;
using Monolith.WebAPI.Data.Journeys;

namespace Monolith.WebAPI.Data.Workspace
{
    public class CustomerFeedback
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int WorkspaceId { get; set; }
        public virtual Workspace Workspace { get; set; }

        [Required]
        public int JourneyId { get; set; }
        public virtual Journey Journey { get; set; }

        [Required]
        public int JourneyStopId { get; set; }
        public virtual JourneyStop JourneyStop { get; set; }

        [Required]
        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; }

        [Required]
        [Range(1, 5)]
        public int OverallRating { get; set; }

        [Range(1, 5)]
        public int? DeliverySpeedRating { get; set; }

        [Range(1, 5)]
        public int? DriverBehaviorRating { get; set; }

        [Range(1, 5)]
        public int? PackageConditionRating { get; set; }

        [MaxLength(1000)]
        public string Comments { get; set; }

        [Required]
        [MaxLength(100)]
        public string FeedbackToken { get; set; }

        // Değerlendirmeyi yapan kişinin bilgileri
        [MaxLength(200)]
        public string SubmitterName { get; set; }

        [MaxLength(100)]
        public string SubmitterEmail { get; set; }

        [MaxLength(20)]
        public string SubmitterPhone { get; set; }

        [MaxLength(50)]
        public string IpAddress { get; set; }

        [MaxLength(500)]
        public string UserAgent { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}