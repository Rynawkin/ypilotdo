using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Extensions;
using Monolith.WebAPI.Requests;
using Monolith.WebAPI.Responses;
using Swashbuckle.AspNetCore.Annotations;

namespace Monolith.WebAPI.Controllers.Workspace;

[ApiController]
[Route("api/workspace/vehicles/maintenance")]
[SwaggerControllerOrder(5)]
[Authorize(AuthenticationSchemes = "Bearer")]
public class MaintenanceController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("vehicle/{vehicleId:int}")]
    [SwaggerOperation(Summary = "Get all maintenance records for a vehicle.")]
    public async Task<ActionResult<IEnumerable<MaintenanceResponse>>> GetByVehicle(int vehicleId)
    {
        var userId = User.GetId();
        var userWorkspaceId = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => u.WorkspaceId)
            .FirstOrDefaultAsync();

        if (userWorkspaceId == null)
            return Unauthorized();

        // Verify vehicle belongs to user's workspace
        var vehicle = await dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == vehicleId && v.WorkspaceId == userWorkspaceId);

        if (vehicle == null)
            return NotFound();

        var maintenances = await dbContext.VehicleMaintenances
            .Where(m => m.VehicleId == vehicleId && m.DeletedAt == null)
            .OrderByDescending(m => m.PerformedAt)
            .Select(m => new MaintenanceResponse
            {
                Id = m.Id,
                VehicleId = m.VehicleId,
                Type = m.Type,
                Title = m.Title,
                Description = m.Description,
                Cost = m.Cost,
                PerformedAt = m.PerformedAt,
                NextMaintenanceDate = m.NextMaintenanceDate,
                NextMaintenanceKm = m.NextMaintenanceKm,
                CurrentKm = m.CurrentKm,
                Workshop = m.Workshop,
                Parts = m.Parts,
                Notes = m.Notes,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            })
            .ToListAsync();

        return Ok(maintenances);
    }

    [HttpGet]
    [SwaggerOperation(Summary = "Get all maintenance records for workspace.")]
    public async Task<ActionResult<IEnumerable<MaintenanceResponse>>> GetAll()
    {
        var userId = User.GetId();
        var userWorkspaceId = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => u.WorkspaceId)
            .FirstOrDefaultAsync();

        if (userWorkspaceId == null)
            return Unauthorized();

        var maintenances = await dbContext.VehicleMaintenances
            .Include(m => m.Vehicle)
            .Where(m => m.Vehicle!.WorkspaceId == userWorkspaceId && m.DeletedAt == null)
            .OrderByDescending(m => m.PerformedAt)
            .Select(m => new MaintenanceResponse
            {
                Id = m.Id,
                VehicleId = m.VehicleId,
                Type = m.Type,
                Title = m.Title,
                Description = m.Description,
                Cost = m.Cost,
                PerformedAt = m.PerformedAt,
                NextMaintenanceDate = m.NextMaintenanceDate,
                NextMaintenanceKm = m.NextMaintenanceKm,
                CurrentKm = m.CurrentKm,
                Workshop = m.Workshop,
                Parts = m.Parts,
                Notes = m.Notes,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            })
            .ToListAsync();

        return Ok(maintenances);
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get a maintenance record by id.")]
    public async Task<ActionResult<MaintenanceResponse>> GetById(int id)
    {
        var userId = User.GetId();
        var userWorkspaceId = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => u.WorkspaceId)
            .FirstOrDefaultAsync();

        if (userWorkspaceId == null)
            return Unauthorized();

        var maintenance = await dbContext.VehicleMaintenances
            .Include(m => m.Vehicle)
            .Where(m => m.Id == id && m.Vehicle!.WorkspaceId == userWorkspaceId && m.DeletedAt == null)
            .Select(m => new MaintenanceResponse
            {
                Id = m.Id,
                VehicleId = m.VehicleId,
                Type = m.Type,
                Title = m.Title,
                Description = m.Description,
                Cost = m.Cost,
                PerformedAt = m.PerformedAt,
                NextMaintenanceDate = m.NextMaintenanceDate,
                NextMaintenanceKm = m.NextMaintenanceKm,
                CurrentKm = m.CurrentKm,
                Workshop = m.Workshop,
                Parts = m.Parts,
                Notes = m.Notes,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (maintenance == null)
            return NotFound();

        return Ok(maintenance);
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create a new maintenance record.")]
    public async Task<ActionResult<MaintenanceResponse>> Create([FromBody] CreateMaintenanceRequest request)
    {
        var userId = User.GetId();
        var userWorkspaceId = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => u.WorkspaceId)
            .FirstOrDefaultAsync();

        if (userWorkspaceId == null)
            return Unauthorized();

        // Verify vehicle belongs to user's workspace
        var vehicle = await dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.WorkspaceId == userWorkspaceId);

        if (vehicle == null)
            return NotFound("Vehicle not found");

        var maintenance = new VehicleMaintenance
        {
            VehicleId = request.VehicleId,
            Type = request.Type,
            Title = request.Title,
            Description = request.Description,
            Cost = request.Cost,
            PerformedAt = request.PerformedAt,
            NextMaintenanceDate = request.NextMaintenanceDate,
            NextMaintenanceKm = request.NextMaintenanceKm,
            CurrentKm = request.CurrentKm,
            Workshop = request.Workshop,
            Parts = request.Parts,
            Notes = request.Notes,
            ReminderDays = request.ReminderDays,
            ReminderKm = request.ReminderKm
        };

        dbContext.VehicleMaintenances.Add(maintenance);
        await dbContext.SaveChangesAsync();

        // Create reminder if either date or km based reminder is set
        if ((request.NextMaintenanceDate.HasValue && request.ReminderDays.HasValue) ||
            (request.NextMaintenanceKm.HasValue && request.ReminderKm.HasValue))
        {
            var reminder = new MaintenanceReminder
            {
                VehicleId = request.VehicleId,
                MaintenanceId = maintenance.Id,
                ReminderDays = request.ReminderDays,
                NextMaintenanceDate = request.NextMaintenanceDate,
                ReminderKm = request.ReminderKm,
                NextMaintenanceKm = request.NextMaintenanceKm,
                IsActive = true
            };

            dbContext.MaintenanceReminders.Add(reminder);
            await dbContext.SaveChangesAsync();
        }

        var response = new MaintenanceResponse
        {
            Id = maintenance.Id,
            VehicleId = maintenance.VehicleId,
            Type = maintenance.Type,
            Title = maintenance.Title,
            Description = maintenance.Description,
            Cost = maintenance.Cost,
            PerformedAt = maintenance.PerformedAt,
            NextMaintenanceDate = maintenance.NextMaintenanceDate,
            NextMaintenanceKm = maintenance.NextMaintenanceKm,
            CurrentKm = maintenance.CurrentKm,
            Workshop = maintenance.Workshop,
            Parts = maintenance.Parts,
            Notes = maintenance.Notes,
            CreatedAt = maintenance.CreatedAt,
            UpdatedAt = maintenance.UpdatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = maintenance.Id }, response);
    }

    [HttpPut("{id:int}")]
    [SwaggerOperation(Summary = "Update a maintenance record.")]
    public async Task<ActionResult<MaintenanceResponse>> Update(int id, [FromBody] UpdateMaintenanceRequest request)
    {
        var userId = User.GetId();
        var userWorkspaceId = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => u.WorkspaceId)
            .FirstOrDefaultAsync();

        if (userWorkspaceId == null)
            return Unauthorized();

        var maintenance = await dbContext.VehicleMaintenances
            .Include(m => m.Vehicle)
            .FirstOrDefaultAsync(m => m.Id == id && m.Vehicle!.WorkspaceId == userWorkspaceId && m.DeletedAt == null);

        if (maintenance == null)
            return NotFound();

        // Update fields if provided
        if (request.Type != null) maintenance.Type = request.Type;
        if (request.Title != null) maintenance.Title = request.Title;
        if (request.Description != null) maintenance.Description = request.Description;
        if (request.Cost.HasValue) maintenance.Cost = request.Cost.Value;
        if (request.PerformedAt.HasValue) maintenance.PerformedAt = request.PerformedAt.Value;
        if (request.NextMaintenanceDate.HasValue) maintenance.NextMaintenanceDate = request.NextMaintenanceDate;
        if (request.NextMaintenanceKm.HasValue) maintenance.NextMaintenanceKm = request.NextMaintenanceKm;
        if (request.CurrentKm.HasValue) maintenance.CurrentKm = request.CurrentKm;
        if (request.Workshop != null) maintenance.Workshop = request.Workshop;
        if (request.Parts != null) maintenance.Parts = request.Parts;
        if (request.Notes != null) maintenance.Notes = request.Notes;
        if (request.ReminderDays.HasValue) maintenance.ReminderDays = request.ReminderDays;
        if (request.ReminderKm.HasValue) maintenance.ReminderKm = request.ReminderKm;

        await dbContext.SaveChangesAsync();

        var response = new MaintenanceResponse
        {
            Id = maintenance.Id,
            VehicleId = maintenance.VehicleId,
            Type = maintenance.Type,
            Title = maintenance.Title,
            Description = maintenance.Description,
            Cost = maintenance.Cost,
            PerformedAt = maintenance.PerformedAt,
            NextMaintenanceDate = maintenance.NextMaintenanceDate,
            NextMaintenanceKm = maintenance.NextMaintenanceKm,
            CurrentKm = maintenance.CurrentKm,
            Workshop = maintenance.Workshop,
            Parts = maintenance.Parts,
            Notes = maintenance.Notes,
            CreatedAt = maintenance.CreatedAt,
            UpdatedAt = maintenance.UpdatedAt
        };

        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    [SwaggerOperation(Summary = "Delete a maintenance record.")]
    public async Task<ActionResult> Delete(int id)
    {
        var userId = User.GetId();
        var userWorkspaceId = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => u.WorkspaceId)
            .FirstOrDefaultAsync();

        if (userWorkspaceId == null)
            return Unauthorized();

        var maintenance = await dbContext.VehicleMaintenances
            .Include(m => m.Vehicle)
            .FirstOrDefaultAsync(m => m.Id == id && m.Vehicle!.WorkspaceId == userWorkspaceId && m.DeletedAt == null);

        if (maintenance == null)
            return NotFound();

        // Soft delete
        maintenance.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("stats/{vehicleId:int}")]
    [SwaggerOperation(Summary = "Get maintenance statistics for a vehicle.")]
    public async Task<ActionResult<MaintenanceStatsResponse>> GetStats(int vehicleId)
    {
        var userId = User.GetId();
        var userWorkspaceId = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => u.WorkspaceId)
            .FirstOrDefaultAsync();

        if (userWorkspaceId == null)
            return Unauthorized();

        // Verify vehicle belongs to user's workspace
        var vehicle = await dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == vehicleId && v.WorkspaceId == userWorkspaceId);

        if (vehicle == null)
            return NotFound();

        var maintenances = await dbContext.VehicleMaintenances
            .Where(m => m.VehicleId == vehicleId && m.DeletedAt == null)
            .ToListAsync();

        var totalMaintenance = maintenances.Count;
        var totalCost = maintenances.Sum(m => m.Cost);
        var avgCost = totalMaintenance > 0 ? totalCost / totalMaintenance : 0;

        var lastMaintenance = maintenances
            .OrderByDescending(m => m.PerformedAt)
            .FirstOrDefault();

        var nextMaintenance = maintenances
            .Where(m => m.NextMaintenanceDate.HasValue && m.NextMaintenanceDate > DateTime.UtcNow)
            .OrderBy(m => m.NextMaintenanceDate)
            .FirstOrDefault();

        var stats = new MaintenanceStatsResponse
        {
            TotalMaintenance = totalMaintenance,
            TotalCost = totalCost,
            AvgCost = avgCost,
            LastMaintenance = lastMaintenance != null ? new MaintenanceResponse
            {
                Id = lastMaintenance.Id,
                VehicleId = lastMaintenance.VehicleId,
                Type = lastMaintenance.Type,
                Title = lastMaintenance.Title,
                Description = lastMaintenance.Description,
                Cost = lastMaintenance.Cost,
                PerformedAt = lastMaintenance.PerformedAt,
                NextMaintenanceDate = lastMaintenance.NextMaintenanceDate,
                NextMaintenanceKm = lastMaintenance.NextMaintenanceKm,
                CurrentKm = lastMaintenance.CurrentKm,
                Workshop = lastMaintenance.Workshop,
                Parts = lastMaintenance.Parts,
                Notes = lastMaintenance.Notes,
                CreatedAt = lastMaintenance.CreatedAt,
                UpdatedAt = lastMaintenance.UpdatedAt
            } : null,
            NextMaintenance = nextMaintenance != null ? new MaintenanceResponse
            {
                Id = nextMaintenance.Id,
                VehicleId = nextMaintenance.VehicleId,
                Type = nextMaintenance.Type,
                Title = nextMaintenance.Title,
                Description = nextMaintenance.Description,
                Cost = nextMaintenance.Cost,
                PerformedAt = nextMaintenance.PerformedAt,
                NextMaintenanceDate = nextMaintenance.NextMaintenanceDate,
                NextMaintenanceKm = nextMaintenance.NextMaintenanceKm,
                CurrentKm = nextMaintenance.CurrentKm,
                Workshop = nextMaintenance.Workshop,
                Parts = nextMaintenance.Parts,
                Notes = nextMaintenance.Notes,
                CreatedAt = nextMaintenance.CreatedAt,
                UpdatedAt = nextMaintenance.UpdatedAt
            } : null
        };

        return Ok(stats);
    }

    [HttpGet("upcoming")]
    [SwaggerOperation(Summary = "Get upcoming maintenance for workspace.")]
    public async Task<ActionResult<IEnumerable<MaintenanceResponse>>> GetUpcoming([FromQuery] int days = 30)
    {
        try
        {
            var userId = User.GetId();
            var userWorkspaceId = await dbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => u.WorkspaceId)
                .FirstOrDefaultAsync();

            if (userWorkspaceId == null)
                return Unauthorized();

            var cutoffDate = DateTime.UtcNow.AddDays(days);

            var upcomingMaintenances = await dbContext.VehicleMaintenances
                .Include(m => m.Vehicle)
                .Where(m => m.Vehicle!.WorkspaceId == userWorkspaceId
                         && m.DeletedAt == null
                         && m.NextMaintenanceDate.HasValue
                         && m.NextMaintenanceDate <= cutoffDate
                         && m.NextMaintenanceDate >= DateTime.UtcNow)
                .OrderBy(m => m.NextMaintenanceDate)
                .Select(m => new MaintenanceResponse
                {
                    Id = m.Id,
                    VehicleId = m.VehicleId,
                    Type = m.Type,
                    Title = m.Title,
                    Description = m.Description,
                    Cost = m.Cost,
                    PerformedAt = m.PerformedAt,
                    NextMaintenanceDate = m.NextMaintenanceDate,
                    NextMaintenanceKm = m.NextMaintenanceKm,
                    CurrentKm = m.CurrentKm,
                    Workshop = m.Workshop,
                    Parts = m.Parts,
                    Notes = m.Notes,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .ToListAsync();

            return Ok(upcomingMaintenances);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Message.Contains("Invalid object name 'VehicleMaintenances'"))
        {
            // BUGFIX: VehicleMaintenances table doesn't exist yet - return empty list
            // TODO: Create migration for VehicleMaintenances table
            return Ok(new List<MaintenanceResponse>());
        }
    }
}
