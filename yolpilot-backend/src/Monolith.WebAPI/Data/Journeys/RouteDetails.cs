using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monolith.WebAPI.Data.Journeys;

public class RouteStartDetails
{
    public int Id { get; set; }
    public TimeSpan StartTime { get; set; }
    
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;
    
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    
    public int RouteId { get; set; }
    
    [ForeignKey("RouteId")]
    public Route Route { get; set; } = null!;
    
    [NotMapped]
    public string LatLng => $"{Latitude},{Longitude}";
}

public class RouteEndDetails
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public TimeSpan? EstimatedArrivalTime { get; set; }

    public int RouteId { get; set; }

    [ForeignKey("RouteId")]
    public Route Route { get; set; } = null!;

    [NotMapped]
    public string LatLng => $"{Latitude},{Longitude}";
}