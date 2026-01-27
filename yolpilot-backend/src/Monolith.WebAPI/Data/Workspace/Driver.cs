// src/Monolith.WebAPI/Data/Workspace/Driver.cs

using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using System.ComponentModel.DataAnnotations;

namespace Monolith.WebAPI.Data.Workspace;

public class Driver : BaseEntity
{
    [Required]
    public int WorkspaceId { get; set; }
    
    // ✅ YENİ: Driver'ı ApplicationUser ile ilişkilendirmek için
    public Guid? UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Email { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string LicenseNumber { get; set; } = string.Empty;
    
    public int? VehicleId { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "available"; // available, busy, offline
    
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    
    [MaxLength(255)]
    public string? Avatar { get; set; }
    
    public double? Rating { get; set; }
    
    public int TotalDeliveries { get; set; } = 0;
    
    // Relationships
    public virtual Workspace? Workspace { get; set; }
    public virtual Vehicle? Vehicle { get; set; }
    public virtual Members.ApplicationUser? User { get; set; } // ✅ YENİ
}