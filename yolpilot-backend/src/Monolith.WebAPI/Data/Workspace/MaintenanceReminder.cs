// src/Monolith.WebAPI/Data/Workspace/MaintenanceReminder.cs

using System.ComponentModel.DataAnnotations;

namespace Monolith.WebAPI.Data.Workspace;

public class MaintenanceReminder : BaseEntity
{
    [Required]
    public int VehicleId { get; set; }

    public int? MaintenanceId { get; set; }

    public int? ReminderDays { get; set; } // Kaç gün öncesinden hatırlatma gönderilsin (tarih bazlı)

    public int? ReminderKm { get; set; } // Kaç km öncesinden hatırlatma gönderilsin (km bazlı)

    public DateTime? NextMaintenanceDate { get; set; } // Sonraki bakım tarihi (tarih bazlı)

    public int? NextMaintenanceKm { get; set; } // Sonraki bakım kilometresi (km bazlı)

    public bool IsActive { get; set; } = true;

    public DateTime? SentAt { get; set; }

    // Relationships
    public virtual Vehicle? Vehicle { get; set; }
    public virtual VehicleMaintenance? Maintenance { get; set; }
}
