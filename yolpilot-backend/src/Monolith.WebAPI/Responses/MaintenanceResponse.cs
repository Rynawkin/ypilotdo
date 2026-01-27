// src/Monolith.WebAPI/Responses/MaintenanceResponse.cs

namespace Monolith.WebAPI.Responses;

public class MaintenanceResponse
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Cost { get; set; }
    public DateTime? PerformedAt { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public int? NextMaintenanceKm { get; set; }
    public int? CurrentKm { get; set; }
    public string? Workshop { get; set; }
    public string? Parts { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class MaintenanceStatsResponse
{
    public int TotalMaintenance { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AvgCost { get; set; }
    public MaintenanceResponse? LastMaintenance { get; set; }
    public MaintenanceResponse? NextMaintenance { get; set; }
}
