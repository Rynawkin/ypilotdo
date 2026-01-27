// src/Monolith.WebAPI/Requests/UpdateMaintenanceRequest.cs

using System.ComponentModel.DataAnnotations;

namespace Monolith.WebAPI.Requests;

public class UpdateMaintenanceRequest
{
    [MaxLength(50)]
    public string Type { get; set; }

    [MaxLength(200)]
    public string Title { get; set; }

    [MaxLength(1000)]
    public string Description { get; set; }

    public decimal? Cost { get; set; }

    public DateTime? PerformedAt { get; set; }

    public DateTime? NextMaintenanceDate { get; set; }

    public int? NextMaintenanceKm { get; set; }

    public int? CurrentKm { get; set; }

    [MaxLength(200)]
    public string Workshop { get; set; }

    [MaxLength(500)]
    public string Parts { get; set; }

    [MaxLength(1000)]
    public string Notes { get; set; }

    public int? ReminderDays { get; set; }

    public int? ReminderKm { get; set; }
}
