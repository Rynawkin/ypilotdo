using System.ComponentModel.DataAnnotations;

namespace Monolith.WebAPI.Data.Marketing;

public class MarketingLead : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Company { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(50)]
    public string? VehicleCount { get; set; }

    [MaxLength(1000)]
    public string? Message { get; set; }

    [MaxLength(50)]
    public string Source { get; set; } = "website"; // website, pricing_page, contact_form

    [MaxLength(50)]
    public string? SelectedPlan { get; set; } // pilot, kaptan, amiral, filo

    public LeadStatus Status { get; set; } = LeadStatus.New;

    [MaxLength(1000)]
    public string? AdminNotes { get; set; }

    public DateTime? ContactedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    [MaxLength(255)]
    public string? AssignedTo { get; set; } // Admin user email who is handling this lead
}

public enum LeadStatus
{
    New = 0,        // Yeni gelen
    Contacted = 1,  // İletişime geçildi
    Qualified = 2,  // Nitelikli prospect
    Demo = 3,       // Demo yapıldı
    Proposal = 4,   // Teklif gönderildi
    Won = 5,        // Kazanıldı (müşteri oldu)
    Lost = 6,       // Kaybedildi
    Archived = 7    // Arşivlendi
}