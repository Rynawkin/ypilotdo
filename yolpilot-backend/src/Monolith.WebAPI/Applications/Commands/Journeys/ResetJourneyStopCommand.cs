// src/Monolith.WebAPI/Applications/Commands/Journeys/ResetJourneyStopCommand.cs

using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class ResetJourneyStopCommand : BaseAuthenticatedCommand<bool>
{
    [JsonIgnore] public override bool RequiresDriver => true;
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;
    [JsonIgnore] public int JourneyId { get; set; }
    [JsonIgnore] public int StopId { get; set; }
}

public class ResetJourneyStopCommandValidator : AbstractValidator<ResetJourneyStopCommand>
{
    public ResetJourneyStopCommandValidator()
    {
        RuleFor(x => x.JourneyId).GreaterThan(0);
        RuleFor(x => x.StopId).GreaterThan(0);
    }
}

public class ResetJourneyStopCommandHandler : BaseAuthenticatedCommandHandler<ResetJourneyStopCommand, bool>
{
    private readonly AppDbContext _context;
    private readonly ILogger<ResetJourneyStopCommandHandler> _logger;

    public ResetJourneyStopCommandHandler(
        AppDbContext context,
        IUserService userService,
        ILogger<ResetJourneyStopCommandHandler> logger)
        : base(userService)
    {
        _context = context;
        _logger = logger;
    }

    protected override async Task<bool> HandleCommand(ResetJourneyStopCommand request, CancellationToken cancellationToken)
    {
        // Journey'yi workspace kontrolü ile al
        var journey = await _context.Journeys
            .Include(x => x.Route)
            .Include(x => x.Stops)
            .FirstOrDefaultAsync(x => 
                x.Id == request.JourneyId && 
                x.Route.WorkspaceId == User.WorkspaceId, 
                cancellationToken);

        if (journey == null)
            throw new ApiException("Journey not found.", 404);

        // ✅ ÖNEMLİ: Sadece devam eden journey'lerde reset yapılabilir
        if (journey.Status != JourneyStatusEnum.InProgress)
        {
            throw new ApiException(
                "Stop can only be reset in active journeys. This journey status is: " + journey.Status.ToString(), 
                400);
        }

        // Driver'lar sadece kendi journey'lerini reset edebilir
        if (User.IsDriver && !User.IsDispatcher && !User.IsAdmin && !User.IsSuperAdmin)
        {
            var driver = await _context.Set<Data.Workspace.Driver>()
                .FirstOrDefaultAsync(d => d.WorkspaceId == User.WorkspaceId && 
                                         d.Email == User.Email,
                                         cancellationToken);
            
            if (driver == null || journey.DriverId != driver.Id)
                throw new ApiException("You can only reset stops for your own journeys.", 403);
        }

        // Stop'u bul
        var stop = await _context.JourneyStops
            .FirstOrDefaultAsync(s => 
                s.Id == request.StopId && 
                s.JourneyId == request.JourneyId, 
                cancellationToken);
        
        if (stop == null)
            throw new ApiException("Stop not found in this journey.", 404);

        // Sadece Failed veya InProgress durumdaki stop'lar reset edilebilir
        if (stop.Status != JourneyStopStatus.Failed && stop.Status != JourneyStopStatus.InProgress)
        {
            throw new ApiException(
                $"Cannot reset stop with status: {stop.Status}. Only Failed or InProgress stops can be reset.", 
                400);
        }

        // Stop'u reset et - Pending durumuna geri al
        stop.Status = JourneyStopStatus.Pending;
        stop.CheckInTime = null;
        stop.CheckOutTime = null;
        stop.UpdatedAt = DateTime.UtcNow;

        // İlgili journey status kayıtlarını temizle (opsiyonel)
        var statusesToRemove = await _context.JourneyStatuses
            .Where(js => js.JourneyId == request.JourneyId && js.StopId == request.StopId)
            .ToListAsync(cancellationToken);

        if (statusesToRemove.Any())
        {
            _context.JourneyStatuses.RemoveRange(statusesToRemove);
            _logger.LogInformation(
                "Removed {Count} status records for journey {JourneyId}, stop {StopId}", 
                statusesToRemove.Count, 
                request.JourneyId, 
                request.StopId);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Stop {StopId} in journey {JourneyId} has been reset to Pending status", 
            request.StopId, 
            request.JourneyId);

        return true;
    }
}