using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Responses.Reports;
using Monolith.WebAPI.Services.Members;
using RouteEntity = Monolith.WebAPI.Data.Journeys.Route;

namespace Monolith.WebAPI.Applications.Queries.Reports;

public class GetVehicleUtilizationQuery : BaseAuthenticatedCommand<IEnumerable<VehicleUtilizationResponse>>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    
    public override bool RequiresDriver => true;
    public override bool RequiresDispatcher => false;
    public override bool RequiresAdmin => false;
    public override bool RequiresSuperAdmin => false;
    public override bool RequiresWorkspaceAccess => false;
}

public class GetVehicleUtilizationQueryHandler : BaseAuthenticatedCommandHandler<GetVehicleUtilizationQuery, IEnumerable<VehicleUtilizationResponse>>
{
    private readonly AppDbContext _context;

    public GetVehicleUtilizationQueryHandler(IUserService userService, AppDbContext context) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<IEnumerable<VehicleUtilizationResponse>> HandleCommand(GetVehicleUtilizationQuery request, CancellationToken cancellationToken)
    {
        var fromDate = request.FromDate ?? DateTime.Today.AddDays(-30);
        var toDate = request.ToDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);
        var totalDays = (toDate - fromDate).Days;

        // Araçları al
        var vehicles = await _context.Vehicles
            .Where(v => v.WorkspaceId == User.WorkspaceId)
            .ToListAsync(cancellationToken);

        // PERFORMANCE: Fetch all routes in a single query to avoid N+1 problem
        var vehicleIds = vehicles.Select(v => v.Id).ToList();
        var routesQuery = _context.Routes
            .Include(r => r.Journeys)
                .ThenInclude(j => j.Stops)
            .Where(r => r.VehicleId.HasValue && vehicleIds.Contains(r.VehicleId.Value) &&
                       r.CreatedAt >= fromDate &&
                       r.CreatedAt <= toDate);

        // Driver ise sadece kendi kullandığı araç verilerini görebilir
        if (User.IsDriver && !User.IsDispatcher && !User.IsAdmin && !User.IsSuperAdmin)
        {
            routesQuery = routesQuery.Where(r => r.DriverId.HasValue &&
                _context.Drivers.Any(d => d.Id == r.DriverId.Value && d.UserId == User.Id));
        }

        var allRoutes = await routesQuery.AsNoTracking().ToListAsync(cancellationToken);

        // Group routes by vehicle (VehicleId is already filtered for HasValue)
        var routesByVehicle = allRoutes.GroupBy(r => r.VehicleId!.Value)
                                       .ToDictionary(g => g.Key, g => g.ToList());

        var utilization = new List<VehicleUtilizationResponse>();

        foreach (var vehicle in vehicles)
        {
            var routes = routesByVehicle.GetValueOrDefault(vehicle.Id, new List<RouteEntity>());

            var totalRoutes = routes.Count;
            var totalDeliveries = routes
                .SelectMany(r => r.Journeys)
                .SelectMany(j => j.Stops)
                .Count(s => s.Status == JourneyStopStatus.Completed); // ✅ Artık JourneyStopStatus tanımlı
            
            var totalDistance = routes.Sum(r => r.TotalDistance ?? 0);
            
            // Kullanım oranı: Günlük ortalama rota sayısına göre hesapla
            var dailyAverage = totalDays > 0 ? (double)totalRoutes / totalDays : 0;
            var utilizationRate = Math.Min(100, dailyAverage * 100 / 3); // Günde 3 rota = %100 kullanım

            utilization.Add(new VehicleUtilizationResponse
            {
                VehicleId = vehicle.Id,
                PlateNumber = vehicle.PlateNumber,
                VehicleType = vehicle.Type,
                TotalRoutes = totalRoutes,
                TotalDeliveries = totalDeliveries,
                TotalDistance = Math.Round(totalDistance, 2),
                UtilizationRate = Math.Round(utilizationRate, 2),
                Status = vehicle.Status
            });
        }

        return utilization.OrderByDescending(v => v.UtilizationRate);
    }
}