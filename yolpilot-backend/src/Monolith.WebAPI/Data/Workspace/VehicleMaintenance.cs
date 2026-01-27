// src/Monolith.WebAPI/Data/Workspace/VehicleMaintenance.cs

using System.ComponentModel.DataAnnotations;

namespace Monolith.WebAPI.Data.Workspace;

public class VehicleMaintenance : BaseEntity
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
    public string? Description { get; set; }

    [Required]
    public decimal Cost { get; set; }

    [Required]
    public DateTime PerformedAt { get; set; }

    public DateTime? NextMaintenanceDate { get; set; }

    public int? NextMaintenanceKm { get; set; }

    public int? CurrentKm { get; set; }

    [MaxLength(200)]
    public string? Workshop { get; set; }

    [MaxLength(500)]
    public string? Parts { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public int? ReminderDays { get; set; }

    public int? ReminderKm { get; set; } // Reminder kilometers before next maintenance (e.g., 1000, 3000, 5000)

    public DateTime? DeletedAt { get; set; } // Soft delete support

    // Relationships
    public virtual Vehicle? Vehicle { get; set; }
    public virtual ICollection<MaintenanceReminder> Reminders { get; set; } = new List<MaintenanceReminder>();
}
