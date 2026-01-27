// src/Monolith.WebAPI/Responses/Workspace/DriverResponse.cs

using System;

namespace Monolith.WebAPI.Responses.Workspace;

public class DriverResponse
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string LicenseNumber { get; set; }
    public string VehicleId { get; set; }
    public string Status { get; set; }
    public LocationDto CurrentLocation { get; set; }
    public string Avatar { get; set; }
    public double? Rating { get; set; }
    public int TotalDeliveries { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // ✅ Login bilgileri (sadece create response'unda dolu olacak)
    public string LoginEmail { get; set; }
    public bool IsUserCreated { get; set; }
    public bool PasswordGenerated { get; set; }  // ✅ EKLENEN
    public string GeneratedPassword { get; set; }  // ✅ EKLENEN
}

public class LocationDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}