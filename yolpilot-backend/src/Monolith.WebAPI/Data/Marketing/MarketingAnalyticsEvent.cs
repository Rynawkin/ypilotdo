using System.ComponentModel.DataAnnotations;

namespace Monolith.WebAPI.Data.Marketing;

public class MarketingAnalyticsEvent : BaseEntity
{
    [Required]
    [MaxLength(64)]
    public string VisitorId { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? EventName { get; set; }

    [MaxLength(255)]
    public string? PagePath { get; set; }

    [MaxLength(200)]
    public string? PageTitle { get; set; }

    [MaxLength(500)]
    public string? Referrer { get; set; }

    [MaxLength(100)]
    public string? UtmSource { get; set; }

    [MaxLength(100)]
    public string? UtmMedium { get; set; }

    [MaxLength(100)]
    public string? UtmCampaign { get; set; }

    [MaxLength(100)]
    public string? UtmContent { get; set; }

    [MaxLength(100)]
    public string? UtmTerm { get; set; }

    [MaxLength(50)]
    public string? DeviceType { get; set; }

    [MaxLength(120)]
    public string? Browser { get; set; }

    [MaxLength(120)]
    public string? Os { get; set; }

    [MaxLength(128)]
    public string? IpHash { get; set; }

    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [MaxLength(8)]
    public string? CountryCode { get; set; }

    [MaxLength(120)]
    public string? CountryName { get; set; }

    [MaxLength(120)]
    public string? Region { get; set; }

    [MaxLength(120)]
    public string? City { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public int? LeadId { get; set; }
    public virtual MarketingLead? Lead { get; set; }

    [MaxLength(4000)]
    public string? MetadataJson { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
