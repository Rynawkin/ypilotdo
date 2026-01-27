// src/Monolith.WebAPI/Requests/CreateMaintenanceRequest.cs

using System.ComponentModel.DataAnnotations;

namespace Monolith.WebAPI.Requests;

public class CreateMaintenanceRequest
{
    [Required]
    public int VehicleId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // routine, repair, inspection, tire_change, oil_change, other

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; }

    [Required]
    public decimal Cost { get; set; }

    [Required]
    public DateTime PerformedAt { get; set; }

    public DateTime? NextMaintenanceDate { get; set; }

    public int? NextMaintenanceKm { get; set; }

    public int? CurrentKm { get; set; }

    [MaxLength(200)]
    public string Workshop { get; set; }

    [MaxLength(500)]
    public string Parts { get; set; }

    [MaxLength(1000)]
    public string Notes { get; set; }

    public int? ReminderDays { get; set; } // Hatırlatma için kaç gün öncesinden (3, 7, 14, 30)

    public int? ReminderKm { get; set; } // Hatırlatma için kaç km öncesinden (1000, 3000, 5000)
}
