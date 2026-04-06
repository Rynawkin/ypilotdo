// src/Monolith.WebAPI/Hubs/TrackingHub.cs
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;

namespace Monolith.WebAPI.Hubs
{
    [Authorize]
    public class TrackingHub : Hub
    {
        private readonly ILogger<TrackingHub> _logger;
        private readonly AppDbContext _context;
        
        public TrackingHub(ILogger<TrackingHub> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var workspaceId = Context.User?.FindFirst("WorkspaceId")?.Value;
            
            if (!string.IsNullOrEmpty(workspaceId))
            {
                // Join workspace tracking group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"tracking-workspace-{workspaceId}");
                _logger.LogInformation("User {UserId} connected to tracking for workspace {WorkspaceId}", 
                    userId, workspaceId);
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                _logger.LogError(exception, "User disconnected with error: {ConnectionId}", Context.ConnectionId);
            }
            else
            {
                _logger.LogInformation("User disconnected: {ConnectionId}", Context.ConnectionId);
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        // Driver/Vehicle tracking group
        public async Task JoinVehicleTracking(int vehicleId)
        {
            var workspaceId = GetWorkspaceIdOrThrow();
            var vehicleExists = await _context.Vehicles
                .AsNoTracking()
                .AnyAsync(v => v.Id == vehicleId && v.WorkspaceId == workspaceId);

            if (!vehicleExists)
            {
                throw new HubException("Vehicle not found.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"tracking-vehicle-{vehicleId}");
            _logger.LogInformation("Connection {ConnectionId} joined vehicle-{VehicleId} tracking", 
                Context.ConnectionId, vehicleId);
        }

        public async Task LeaveVehicleTracking(int vehicleId)
        {
            var workspaceId = GetWorkspaceIdOrThrow();
            var vehicleExists = await _context.Vehicles
                .AsNoTracking()
                .AnyAsync(v => v.Id == vehicleId && v.WorkspaceId == workspaceId);

            if (!vehicleExists)
            {
                throw new HubException("Vehicle not found.");
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tracking-vehicle-{vehicleId}");
            _logger.LogInformation("Connection {ConnectionId} left vehicle-{VehicleId} tracking", 
                Context.ConnectionId, vehicleId);
        }

        // Join all active journeys tracking for a workspace
        public async Task JoinWorkspaceTracking(int workspaceId)
        {
            var currentWorkspaceId = GetWorkspaceIdOrThrow();
            if (workspaceId != currentWorkspaceId)
            {
                throw new HubException("You can only subscribe to your own workspace.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"tracking-workspace-{workspaceId}");
            _logger.LogInformation("Connection {ConnectionId} joined workspace-{WorkspaceId} tracking", 
                Context.ConnectionId, workspaceId);
        }

        // ✅ DÜZELTME: LeaveWorkspaceTracking metodu eklendi
        public async Task LeaveWorkspaceTracking(int workspaceId)
        {
            var currentWorkspaceId = GetWorkspaceIdOrThrow();
            if (workspaceId != currentWorkspaceId)
            {
                throw new HubException("You can only leave your own workspace tracking group.");
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tracking-workspace-{workspaceId}");
            _logger.LogInformation("Connection {ConnectionId} left workspace-{WorkspaceId} tracking", 
                Context.ConnectionId, workspaceId);
        }

        // Update vehicle location (called by driver mobile app or simulator)
        public async Task UpdateVehicleLocation(UpdateLocationDto locationUpdate)
        {
            try
            {
                var workspaceId = GetWorkspaceIdOrThrow();

                // Validate journey exists and is active
                var journey = await _context.Journeys
                    .Include(j => j.Vehicle)
                    .Include(j => j.Driver)
                    .FirstOrDefaultAsync(j => 
                        j.Id == locationUpdate.JourneyId && 
                        j.WorkspaceId == workspaceId &&
                        j.Status == JourneyStatusEnum.InProgress);

                if (journey == null)
                {
                    _logger.LogWarning("Journey {JourneyId} not found or not active", locationUpdate.JourneyId);
                    return;
                }

                await EnsureDriverOwnsJourneyIfDriverAsync(journey);

                // Update journey's live location
                journey.LiveLocation = new LiveLocation
                {
                    Latitude = locationUpdate.Latitude,
                    Longitude = locationUpdate.Longitude,
                    Speed = locationUpdate.Speed,
                    Heading = locationUpdate.Heading,
                    Accuracy = locationUpdate.Accuracy,
                    Timestamp = DateTime.UtcNow
                };

                await _context.SaveChangesAsync();

                // Broadcast to all tracking this vehicle
                await Clients.Group($"tracking-vehicle-{journey.VehicleId}")
                    .SendAsync("VehicleLocationUpdated", new
                    {
                        journeyId = journey.Id,
                        vehicleId = journey.VehicleId,
                        location = journey.LiveLocation,
                        driverId = journey.DriverId,
                        currentStopIndex = journey.CurrentStopIndex
                    });

                // Broadcast to workspace tracking
                await Clients.Group($"tracking-workspace-{workspaceId}")
                    .SendAsync("WorkspaceVehicleUpdated", new
                    {
                        journeyId = journey.Id,
                        vehicleId = journey.VehicleId,
                        plateNumber = journey.Vehicle?.PlateNumber,
                        location = journey.LiveLocation,
                        driverId = journey.DriverId,
                        currentStopIndex = journey.CurrentStopIndex,
                        totalStops = await _context.JourneyStops.CountAsync(s => s.JourneyId == journey.Id)
                    });

                _logger.LogDebug("Location updated for journey {JourneyId}: Lat={Lat}, Lng={Lng}, Speed={Speed}", 
                    journey.Id, locationUpdate.Latitude, locationUpdate.Longitude, locationUpdate.Speed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vehicle location for journey {JourneyId}", 
                    locationUpdate.JourneyId);
            }
        }

        // ✅ DÜZELTME: AsNoTracking() eklendi ve sorgu basitleştirildi
        public async Task<List<ActiveVehicleDto>> GetActiveVehicles()
        {
            try
            {
                var workspaceId = Context.User?.FindFirst("WorkspaceId")?.Value;
                if (string.IsNullOrEmpty(workspaceId))
                {
                    return new List<ActiveVehicleDto>();
                }

                var workspaceIdInt = int.Parse(workspaceId);

                // ✅ ÇÖZÜM: Önce journeys'i çek, sonra DTO'ya map et
                // ✅ DÜZELTME: Preparing yerine Planned kullanıldı
                var activeJourneys = await _context.Journeys
                    .AsNoTracking() // ✅ ÖNEMLİ: Tracking'i kapat
                    .Include(j => j.Vehicle)
                    .Include(j => j.Driver)
                    .Include(j => j.Stops)
                    .Where(j => 
                        j.WorkspaceId == workspaceIdInt && 
                        (j.Status == JourneyStatusEnum.InProgress || j.Status == JourneyStatusEnum.Planned))
                    .ToListAsync();

                // ✅ Map to DTO after materialization
                var result = activeJourneys
                    .Where(j => j.LiveLocation != null)
                    .Select(j => new ActiveVehicleDto
                    {
                        JourneyId = j.Id,
                        VehicleId = j.VehicleId ?? 0,
                        PlateNumber = j.Vehicle?.PlateNumber ?? string.Empty,
                        DriverId = j.DriverId,
                        DriverName = j.Driver?.Name ?? string.Empty,
                        Location = j.LiveLocation,
                        CurrentStopIndex = j.CurrentStopIndex,
                        TotalStops = j.Stops?.Count ?? 0,
                        StartedAt = j.StartedAt
                    })
                    .ToList();

                _logger.LogInformation("Found {Count} active vehicles for workspace {WorkspaceId}", 
                    result.Count, workspaceId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active vehicles");
                return new List<ActiveVehicleDto>();
            }
        }

        // Send panic/emergency alert
        public async Task SendEmergencyAlert(EmergencyAlertDto alert)
        {
            try
            {
                var workspaceId = GetWorkspaceIdOrThrow();

                var journey = await _context.Journeys
                    .Include(j => j.Driver)
                    .FirstOrDefaultAsync(j => j.Id == alert.JourneyId && j.WorkspaceId == workspaceId);

                if (journey == null)
                {
                    throw new HubException("Journey not found.");
                }

                await EnsureDriverOwnsJourneyIfDriverAsync(journey);

                // Log emergency alert
                _logger.LogWarning("EMERGENCY ALERT from journey {JourneyId}: {Message}", 
                    alert.JourneyId, alert.Message);

                // Notify all workspace admins
                await Clients.Group($"tracking-workspace-{workspaceId}")
                    .SendAsync("EmergencyAlert", new
                    {
                        journeyId = alert.JourneyId,
                        vehicleId = alert.VehicleId,
                        driverId = alert.DriverId,
                        message = alert.Message,
                        location = alert.Location,
                        timestamp = DateTime.UtcNow
                    });

                // TODO: Send SMS/Email notifications to managers
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending emergency alert for journey {JourneyId}", 
                    alert.JourneyId);
            }
        }

    // DTOs for TrackingHub
    public class UpdateLocationDto
    {
        public int JourneyId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Speed { get; set; }
        public double? Heading { get; set; }
        public double? Accuracy { get; set; }
    }

    public class ActiveVehicleDto
    {
        public int JourneyId { get; set; }
        public int VehicleId { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public int DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public LiveLocation? Location { get; set; }
        public int CurrentStopIndex { get; set; }
        public int TotalStops { get; set; }
        public DateTime? StartedAt { get; set; }
    }

    public class EmergencyAlertDto
    {
        public int JourneyId { get; set; }
        public int VehicleId { get; set; }
        public int DriverId { get; set; }
        public string Message { get; set; } = string.Empty;
        public LiveLocation? Location { get; set; }
    }

    private int GetWorkspaceIdOrThrow()
    {
        var workspaceId = Context.User?.FindFirst("WorkspaceId")?.Value;
        if (string.IsNullOrWhiteSpace(workspaceId) || !int.TryParse(workspaceId, out var parsedWorkspaceId))
        {
            throw new HubException("Workspace claim is missing.");
        }

        return parsedWorkspaceId;
    }

    private async Task EnsureDriverOwnsJourneyIfDriverAsync(Data.Journeys.Journey journey)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var parsedUserId))
        {
            return;
        }

        var driver = await _context.Drivers
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.WorkspaceId == journey.WorkspaceId && d.UserId == parsedUserId);

        if (driver != null && journey.DriverId != driver.Id)
        {
            throw new HubException("You can only update your assigned journey.");
        }

}
}
}
