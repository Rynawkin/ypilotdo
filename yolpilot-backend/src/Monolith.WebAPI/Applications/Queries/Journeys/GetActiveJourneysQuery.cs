// src/Monolith.WebAPI/Applications/Queries/Journeys/GetActiveJourneysQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;

namespace Monolith.WebAPI.Applications.Queries.Journeys;

// ✅ LiveTracking için optimize edilmiş DTO
public class ActiveJourneyDto
{
    public int Id { get; set; }
    public int RouteId { get; set; }
    public string RouteName { get; set; }
    public DateTime Date { get; set; }
    public JourneyStatusEnum Status { get; set; }
    
    // Driver
    public int DriverId { get; set; }
    public string DriverName { get; set; }
    
    // Vehicle
    public int? VehicleId { get; set; }
    public string VehiclePlateNumber { get; set; }
    
    // Location
    public LiveLocation? LiveLocation { get; set; }
    public int CurrentStopIndex { get; set; }
    
    // Route basics (LiveMap için)
    public string? Polyline { get; set; }
    public int TotalStops { get; set; }
    
    // Times
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    
    // Route details for map
    public RouteDetailsDto? Route { get; set; }
}

public class RouteDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<StopBasicDto> Stops { get; set; } = new();
    public int? VehicleId { get; set; }
    public int? DriverId { get; set; }
    public VehicleBasicDto? Vehicle { get; set; }
    public DriverBasicDto? Driver { get; set; }
}

public class StopBasicDto
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime? EstimatedArrivalTime { get; set; }
    public bool IsCompleted { get; set; }
}

public class VehicleBasicDto
{
    public int Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}

public class DriverBasicDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public record GetActiveJourneysQuery(int WorkspaceId) : IRequest<IEnumerable<ActiveJourneyDto>>;

public class GetActiveJourneysQueryHandler(AppDbContext context) 
    : IRequestHandler<GetActiveJourneysQuery, IEnumerable<ActiveJourneyDto>>
{
    public async Task<IEnumerable<ActiveJourneyDto>> Handle(
        GetActiveJourneysQuery request, 
        CancellationToken cancellationToken)
    {
        // ✅ Ham veriyi önce çek
        var activeJourneys = await context.Journeys
            .AsNoTracking()
            .Include(j => j.Driver)
            .Include(j => j.Vehicle)
            .Include(j => j.Route)
                .ThenInclude(r => r.Vehicle)
            .Include(j => j.Route)
                .ThenInclude(r => r.Driver)
            .Include(j => j.Route)
                .ThenInclude(r => r.Stops.OrderBy(s => s.Order).Take(50))
            .Include(j => j.Stops.OrderBy(s => s.Order).Take(50))
            .Where(j => j.WorkspaceId == request.WorkspaceId &&
                       (j.Status == JourneyStatusEnum.InProgress || 
                        j.Status == JourneyStatusEnum.Planned))
            .ToListAsync(cancellationToken);

        // ✅ Memory'de DTO'ya map et
        var result = activeJourneys.Select(j => new ActiveJourneyDto
        {
            Id = j.Id,
            RouteId = j.RouteId,
            RouteName = j.Route?.Name ?? $"Route #{j.RouteId}",
            Date = j.Date,
            Status = j.Status,
            
            // Driver
            DriverId = j.DriverId,
            DriverName = j.Driver?.Name ?? "Bilinmiyor",
            
            // Vehicle
            VehicleId = j.VehicleId,
            VehiclePlateNumber = j.Vehicle?.PlateNumber ?? "Atanmadı",
            
            // Location
            LiveLocation = j.LiveLocation,
            CurrentStopIndex = j.CurrentStopIndex,
            
            // Route basics
            Polyline = j.Polyline ?? string.Empty,
            TotalStops = j.Stops?.Count ?? 0,
            
            // Times
            StartedAt = j.StartedAt,
            FinishedAt = j.FinishedAt,
            
            // Route details
            Route = j.Route != null ? new RouteDetailsDto
            {
                Id = j.Route.Id,
                Name = j.Route.Name ?? $"Route #{j.Route.Id}",
                VehicleId = j.Route.VehicleId,
                DriverId = j.Route.DriverId,
                Vehicle = j.Route.Vehicle != null ? new VehicleBasicDto
                {
                    Id = j.Route.Vehicle.Id,
                    PlateNumber = j.Route.Vehicle.PlateNumber,
                    Brand = j.Route.Vehicle.Brand,
                    Model = j.Route.Vehicle.Model
                } : null,
                Driver = j.Route.Driver != null ? new DriverBasicDto
                {
                    Id = j.Route.Driver.Id,
                    Name = j.Route.Driver.Name,
                    Phone = j.Route.Driver.Phone ?? string.Empty
                } : null,
                Stops = j.Route.Stops?.Select(s => new StopBasicDto
                {
                    Id = s.Id,
                    Order = s.Order,
                    Address = s.Address ?? string.Empty,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    EstimatedArrivalTime = null,
                    IsCompleted = false
                }).ToList() ?? new List<StopBasicDto>()
            } : null
        }).ToList();

        return result;
    }
}