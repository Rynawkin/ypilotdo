using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Journeys;

/// <summary>
/// Performans için optimize edilmiş özet journey bilgisi
/// Dashboard ve Journeys listesi için kullanılır
/// </summary>
public class GetJourneysSummaryQuery : BaseAuthenticatedCommand<IEnumerable<JourneySummaryResponse>>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Status { get; set; }
    public int? DriverId { get; set; }

    // Rol yetkilendirmeleri
    public override bool RequiresDriver => true; // Driver ve üstü roller erişebilir
    public override bool RequiresDispatcher => false;
    public override bool RequiresAdmin => false;
    public override bool RequiresSuperAdmin => false;
    public override bool RequiresWorkspaceAccess => false;
}

public class GetJourneysSummaryQueryHandler : BaseAuthenticatedCommandHandler<GetJourneysSummaryQuery, IEnumerable<JourneySummaryResponse>>
{
    private readonly AppDbContext _context;

    public GetJourneysSummaryQueryHandler(IUserService userService, AppDbContext context) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<IEnumerable<JourneySummaryResponse>> HandleCommand(GetJourneysSummaryQuery request, CancellationToken cancellationToken)
    {
        // Tarih aralığını belirle
        static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        var fromDate = EnsureUtc(request.FromDate ?? DateTime.UtcNow.Date.AddDays(-30)); // Son 30 g??n
        var toDate = EnsureUtc(request.ToDate ?? DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1)); // Bug??n 23:59:59

        // Base query
        var query = _context.Journeys
            .Include(j => j.Route)
                .ThenInclude(r => r.Driver)
            .Include(j => j.Route)
                .ThenInclude(r => r.Vehicle)
            .Include(j => j.Driver)
            .Include(j => j.Stops)
            .Where(j => j.Route.WorkspaceId == User.WorkspaceId &&
                       j.CreatedAt >= fromDate &&
                       j.CreatedAt <= toDate);

        // Driver ise sadece kendi journey'lerini görebilir
        if (User.IsDriver && !User.IsDispatcher && !User.IsAdmin && !User.IsSuperAdmin)
        {
            query = query.Where(j => j.Driver != null && j.Driver.UserId == User.Id);
        }

        // Status filtresi
        if (!string.IsNullOrEmpty(request.Status))
        {
            query = request.Status.ToLower() switch
            {
                "archived" => query.Where(j => j.ArchivedAt.HasValue),
                "cancelled" => query.Where(j => j.CancelledAt.HasValue && !j.ArchivedAt.HasValue),
                "completed" => query.Where(j => j.CompletedAt.HasValue && !j.CancelledAt.HasValue && !j.ArchivedAt.HasValue),
                "finished" => query.Where(j => j.FinishedAt.HasValue && !j.CompletedAt.HasValue && !j.CancelledAt.HasValue && !j.ArchivedAt.HasValue),
                "in_progress" => query.Where(j => j.StartedAt.HasValue && !j.FinishedAt.HasValue && !j.CompletedAt.HasValue && !j.CancelledAt.HasValue && !j.ArchivedAt.HasValue),
                "preparing" => query.Where(j => !j.StartedAt.HasValue && !j.CancelledAt.HasValue && !j.ArchivedAt.HasValue),
                _ => query
            };
        }

        // Driver filtresi
        if (request.DriverId.HasValue)
        {
            query = query.Where(j => j.Driver != null && j.Driver.Id == request.DriverId.Value);
        }

        var journeys = await query
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new JourneySummaryResponse
            {
                Id = j.Id,
                RouteId = j.RouteId,
                Name = j.Name,
                RouteName = j.Route.Name,
                Date = j.CreatedAt.ToString("yyyy-MM-dd"),
                
                // Driver bilgileri
                DriverId = j.Driver != null ? j.Driver.Id.ToString() : (j.Route.Driver != null ? j.Route.Driver.Id.ToString() : null),
                DriverName = j.Driver != null ? j.Driver.Name : (j.Route.Driver != null ? j.Route.Driver.Name : "Atanmadı"),
                
                // Vehicle bilgileri
                VehicleId = j.Route.Vehicle != null ? j.Route.Vehicle.Id.ToString() : null,
                VehiclePlateNumber = j.Route.Vehicle != null ? j.Route.Vehicle.PlateNumber : "Atanmadı",
                
                // Metrikler
                TotalStops = j.Stops.Count,
                CompletedStops = j.Stops.Count(s => s.Status == JourneyStopStatus.Completed),
                TotalDistance = j.Route.TotalDistance ?? 0,
                TotalDuration = j.Route.TotalDuration ?? 0,
                
                // Tarihler
                StartedAt = j.StartedAt,
                CompletedAt = j.CompletedAt,
                CancelledAt = j.CancelledAt,
                FinishedAt = j.FinishedAt,
                ArchivedAt = j.ArchivedAt,
                CreatedAt = j.CreatedAt,
                
                // Live location - Driver'dan al
                LiveLocation = j.Driver != null && j.Driver.CurrentLatitude.HasValue && j.Driver.CurrentLongitude.HasValue
                    ? new LocationInfo
                    {
                        Latitude = j.Driver.CurrentLatitude.Value,
                        Longitude = j.Driver.CurrentLongitude.Value,
                        Speed = null
                    }
                    : null
            })
            .ToListAsync(cancellationToken);

        return journeys;
    }
}

/// <summary>
/// Journey özet response modeli - minimal veri içerir
/// </summary>
public class JourneySummaryResponse
{
    public int Id { get; set; }
    public int RouteId { get; set; }
    public string? Name { get; set; }
    public string RouteName { get; set; } = "";
    public string Date { get; set; } = "";
    
    // Status hesaplanmış property olarak
    public string Status 
    { 
        get 
        {
            if (ArchivedAt.HasValue) return "archived";
            if (CancelledAt.HasValue) return "cancelled";
            if (CompletedAt.HasValue) return "completed";
            if (FinishedAt.HasValue) return "finished";
            if (StartedAt.HasValue) return "in_progress";
            return "preparing";
        }
    }
    
    // Driver
    public string? DriverId { get; set; }
    public string DriverName { get; set; } = "";
    
    // Vehicle
    public string? VehicleId { get; set; }
    public string VehiclePlateNumber { get; set; } = "";
    
    // Metrikler
    public int TotalStops { get; set; }
    public int CompletedStops { get; set; }
    public double TotalDistance { get; set; }
    public int TotalDuration { get; set; }
    
    // Tarihler
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Live location
    public LocationInfo? LiveLocation { get; set; }
}

public class LocationInfo
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Speed { get; set; }
}
