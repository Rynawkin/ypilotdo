// src/Monolith.WebAPI/Applications/Commands/Drivers/DriverMapper.cs

using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Responses.Workspace;

namespace Monolith.WebAPI.Applications.Commands.Drivers;

public static class DriverMapper
{
    public static Driver ToEntity(this CreateDriverDto dto, int workspaceId)
    {
        return new Driver
        {
            WorkspaceId = workspaceId,
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            LicenseNumber = dto.LicenseNumber,
            VehicleId = dto.VehicleId,
            Status = dto.Status,
            CurrentLatitude = dto.CurrentLatitude,
            CurrentLongitude = dto.CurrentLongitude,
            Avatar = dto.Avatar,
            Rating = dto.Rating,
            TotalDeliveries = dto.TotalDeliveries,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };
    }

    public static void UpdateEntity(this Driver entity, UpdateDriverDto dto)
    {
        entity.Name = dto.Name;
        entity.Phone = dto.Phone;
        entity.Email = dto.Email;
        entity.LicenseNumber = dto.LicenseNumber;
        entity.VehicleId = dto.VehicleId;
        entity.Status = dto.Status;
        entity.CurrentLatitude = dto.CurrentLatitude;
        entity.CurrentLongitude = dto.CurrentLongitude;
        entity.Avatar = dto.Avatar;
        entity.Rating = dto.Rating;
        entity.TotalDeliveries = dto.TotalDeliveries;
        entity.UpdatedAt = DateTime.UtcNow;
    }

    public static DriverResponse ToResponse(this Driver entity)
    {
        return new DriverResponse
        {
            Id = entity.Id.ToString(),
            Name = entity.Name,
            Phone = entity.Phone,
            Email = entity.Email,
            LicenseNumber = entity.LicenseNumber,
            VehicleId = entity.VehicleId?.ToString(),
            Status = entity.Status,
            CurrentLocation = (entity.CurrentLatitude.HasValue && entity.CurrentLongitude.HasValue) 
                ? new LocationDto 
                { 
                    Latitude = entity.CurrentLatitude.Value, 
                    Longitude = entity.CurrentLongitude.Value 
                } 
                : null,
            Avatar = entity.Avatar,
            Rating = entity.Rating,
            TotalDeliveries = entity.TotalDeliveries,
            CreatedAt = entity.CreatedAt
        };
    }

    public static Driver ToBulkImportEntity(this BulkImportDriverDto dto, int workspaceId)
    {
        return new Driver
        {
            WorkspaceId = workspaceId,
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            LicenseNumber = dto.LicenseNumber,
            Status = dto.Status,
            Rating = dto.Rating,
            TotalDeliveries = dto.TotalDeliveries,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };
    }
}