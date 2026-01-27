using System.ComponentModel.DataAnnotations;
using Monolith.WebAPI.Applications.Commands.Journeys;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Data.Journeys;

public class Route : BaseEntity
{
    // do not remove this constructor
    public Route()
    {
    }

    public Route(CreateRouteCommand command, Depot depot, int workspaceId, TimeSpan defaultServiceTime)
    {
        Name = command.Name;
        DepotId = command.DepotId;
        WorkspaceId = workspaceId;
        Date = command.Date;
        Status = "draft"; // Default status
        DriverId = command.DriverId;
        VehicleId = command.VehicleId;
        Notes = command.Notes ?? "";  // Notes null kontrolü ile
        
        // METRICS - ÖNEMLİ - ✅ DÜZELTİLDİ
        TotalDeliveries = command.Stops?.Count ?? 0;
        CompletedDeliveries = 0;
        TotalDistance = command.TotalDistance ?? 0; // Frontend'den gelen değeri kullan
        TotalDuration = command.TotalDuration ?? 0; // Frontend'den gelen değeri kullan
        Optimized = command.Optimized; // Frontend'den gelen değeri kullan

        // Optimization settings
        AvoidTolls = command.AvoidTolls; // Optimize sırasında kullanılan ayarı sakla

        // STOPS - Order'ları düzgün set et
        if (command.Stops is not null && command.Stops.Count > 0)
        {
            Stops = new List<RouteStop>();
            for (int i = 0; i < command.Stops.Count; i++)
            {
                var stop = new RouteStop(command.Stops[i], Id, defaultServiceTime);
                stop.Order = i + 1; // ÖNEMLİ: Order'ı set et
                Stops.Add(stop);
            }
        }

        // START DETAILS
        if (command.StartDetails is not null)
        {
            StartDetails = new RouteStartDetails
            {
                StartTime = command.StartDetails.StartTime,
                Name = command.StartDetails.Name,
                Address = command.StartDetails.Address,
                Latitude = command.StartDetails.Latitude,
                Longitude = command.StartDetails.Longitude
            };
        }
        else if (depot != null)
        {
            StartDetails = new RouteStartDetails
            {
                StartTime = depot.StartWorkingHours,
                Name = depot.Name,
                Address = depot.Address,
                Latitude = depot.Latitude,
                Longitude = depot.Longitude
            };
        }

        // END DETAILS
        if (command.EndDetails is not null)
        {
            EndDetails = new RouteEndDetails
            {
                Name = command.EndDetails.Name,
                Address = command.EndDetails.Address,
                Latitude = command.EndDetails.Latitude,
                Longitude = command.EndDetails.Longitude
            };
        }
        else if (depot != null)
        {
            EndDetails = new RouteEndDetails
            {
                Name = depot.Name,
                Address = depot.Address,
                Latitude = depot.Latitude,
                Longitude = depot.Longitude
            };
        }
    }

    [Required]
    [MaxLength(200)]
    public string Name { get; private set; }
    
    [Required]
    public DateTime Date { get; private set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "draft"; // draft, planned, in_progress, completed, cancelled

    // Driver relationship - OPTIONAL
    public int? DriverId { get; set; }
    public Driver Driver { get; set; }

    // Vehicle relationship - OPTIONAL
    public int? VehicleId { get; set; }
    public Vehicle Vehicle { get; set; }

    // Route metrics
    public double? TotalDistance { get; set; } // km
    public int? TotalDuration { get; set; } // minutes
    public int TotalDeliveries { get; set; }
    public int CompletedDeliveries { get; set; }
    public bool Optimized { get; set; }

    // Optimization settings
    public bool AvoidTolls { get; set; } = false;
    
    [MaxLength(int.MaxValue)]
    public string Polyline { get; set; }


    // Timestamps for tracking
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Notes
    [MaxLength(500)]
    public string Notes { get; set; }

    [Required]
    public int WorkspaceId { get; private set; }
    public Workspace.Workspace Workspace { get; set; }
    
    [Required]
    public int DepotId { get; private set; }
    public Depot Depot { get; set; }

    public RouteStartDetails StartDetails { get; set; }
    public RouteEndDetails EndDetails { get; set; }

    public ICollection<RouteStop> Stops { get; set; }

    // ReSharper disable once CollectionNeverUpdated.Global
    public ICollection<Journey> Journeys { get; set; }
}