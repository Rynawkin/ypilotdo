// src/Monolith.WebAPI/Data/Workspace/Vehicle.cs

using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using System.ComponentModel.DataAnnotations;

namespace Monolith.WebAPI.Data.Workspace;

public class Vehicle : BaseEntity
{
    [Required]
    public int WorkspaceId { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string PlateNumber { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // car, van, truck, motorcycle
    
    [Required]
    [MaxLength(50)]
    public string Brand { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Model { get; set; } = string.Empty;
    
    public int Year { get; set; }
    
    public int Capacity { get; set; } // kg
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "active"; // active, maintenance, inactive
    
    [MaxLength(20)]
    public string FuelType { get; set; } = "gasoline"; // gasoline, diesel, electric, hybrid

    public int? CurrentKm { get; set; } // Current kilometer reading

    public DateTime? DeletedAt { get; set; } // Soft delete support
    
    // Relationships
    public virtual Workspace? Workspace { get; set; }
    
    // EKLENDI - Journey relationship
    public virtual ICollection<Journey> Journeys { get; set; }
}