using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Services.Members;
using Microsoft.Extensions.Logging;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class OptimizeJourneyForDeviationCommand : BaseAuthenticatedCommand<JourneyResponse>
{
    [JsonIgnore] public override bool RequiresDriver => false;
    [JsonIgnore] public override bool RequiresDispatcher => false;  
    [JsonIgnore] public override bool RequiresWorkspaceAccess => false;
    [JsonIgnore] public int JourneyId { get; set; }
    
    public string ActualStartTime { get; set; } = string.Empty;
}

public class OptimizeJourneyForDeviationCommandValidator : AbstractValidator<OptimizeJourneyForDeviationCommand>
{
    public OptimizeJourneyForDeviationCommandValidator()
    {
        RuleFor(x => x.JourneyId).GreaterThan(0);
        RuleFor(x => x.ActualStartTime)
            .NotEmpty()
            .Matches(@"^\d{2}:\d{2}(:\d{2})?$")
            .WithMessage("ActualStartTime must be in HH:mm or HH:mm:ss format");
    }
}

public class OptimizeJourneyForDeviationCommandHandler : BaseAuthenticatedCommandHandler<OptimizeJourneyForDeviationCommand, JourneyResponse>
{
    private readonly AppDbContext _context;
    private readonly ISender _sender;
    private readonly ILogger<OptimizeJourneyForDeviationCommandHandler> _logger;

    public OptimizeJourneyForDeviationCommandHandler(
        AppDbContext context,
        IUserService userService,
        ISender sender,
        ILogger<OptimizeJourneyForDeviationCommandHandler> logger)
        : base(userService)
    {
        _context = context;
        _sender = sender;
        _logger = logger;
    }

    protected override async Task<JourneyResponse> HandleCommand(
        OptimizeJourneyForDeviationCommand request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== OPTIMIZE JOURNEY FOR DEVIATION START ===");
        _logger.LogInformation($"JourneyId: {request.JourneyId}, ActualStartTime: {request.ActualStartTime}");
        _logger.LogInformation($"User: {User.Id}, IsDriver: {User.IsDriver}, WorkspaceId: {User.WorkspaceId}");

        // Journey'yi tüm ilişkileriyle birlikte yükle
        var journey = await _context.Journeys
            .Include(x => x.Route)
                .ThenInclude(r => r.Stops)
                    .ThenInclude(s => s.Customer)
            .Include(x => x.Route)
                .ThenInclude(r => r.Depot)
            .Include(x => x.Route)
                .ThenInclude(r => r.StartDetails)
            .Include(x => x.Stops)
                .ThenInclude(s => s.RouteStop)
            .Include(x => x.Driver)
            .FirstOrDefaultAsync(
                x => x.Id == request.JourneyId, 
                cancellationToken);

        if (journey == null)
            throw new ApiException("Journey not found.", 404);

        // Workspace kontrolü - journey üzerinden
        if (journey.Route?.WorkspaceId == null)
            throw new ApiException("Journey has no associated workspace.", 400);

        // Yetki kontrolü - Sürücü kendi journey'sini optimize edebilmeli
        if (User.IsDriver && !User.IsDispatcher && !User.IsAdmin)
        {
            if (journey.Driver?.UserId != User.Id)
            {
                throw new ApiException("You can only optimize your own journeys.", 403);
            }
        }

        // Status kontrolü - planned veya in_progress olmalı
        var journeyStatus = journey.Status.ToString().ToLower();
        if (journeyStatus != "planned" && journeyStatus != "inprogress")
            throw new ApiException("Only planned or in-progress journeys can be optimized for time deviation.", 400);

        // Sapma kontrolü
        var plannedStartTime = journey.Route.StartDetails?.StartTime ?? new TimeSpan(9, 0, 0);
        var actualStartTime = TimeSpan.Parse(request.ActualStartTime);
        var deviationMinutes = Math.Abs((actualStartTime - plannedStartTime).TotalMinutes);

        _logger.LogInformation($"Planned start: {plannedStartTime}, Actual start: {actualStartTime}, Deviation: {deviationMinutes} minutes");

        // ✅ YENİ: Delta-based ETA update (Google Maps API kullanmadan)
        // Sadece zaman farkını tüm duraklara uygula
        _logger.LogInformation("Applying launch delay to all pending stops using delta-based calculation...");

        var timeDelta = actualStartTime - plannedStartTime;

        // Route'un StartDetails'ini güncelle
        if (journey.Route.StartDetails != null)
        {
            journey.Route.StartDetails.StartTime = actualStartTime;
            _context.Entry(journey.Route.StartDetails).State = EntityState.Modified;
        }

        // Journey StartDetails'ini güncelle
        if (journey.StartDetails != null)
        {
            journey.StartDetails.StartTime = actualStartTime;
            _context.Entry(journey.StartDetails).State = EntityState.Modified;
        }

        // Sadece bekleyen (pending) durakların ETA'larını güncelle
        var pendingStops = journey.Stops.Where(s => s.Status == Data.Journeys.JourneyStopStatus.Pending).ToList();

        foreach (var journeyStop in pendingStops)
        {
            // Original ETA'ları sakla (ilk kez güncelleniyorsa)
            if (journeyStop.OriginalEstimatedArrivalTime == TimeSpan.Zero)
            {
                journeyStop.OriginalEstimatedArrivalTime = journeyStop.EstimatedArrivalTime;
                journeyStop.OriginalEstimatedDepartureTime = journeyStop.EstimatedDepartureTime ?? TimeSpan.Zero;
            }

            // Current ETA'ları güncelle (time delta ekle/çıkar)
            journeyStop.EstimatedArrivalTime += timeDelta;
            if (journeyStop.EstimatedDepartureTime.HasValue)
            {
                journeyStop.EstimatedDepartureTime = journeyStop.EstimatedDepartureTime.Value + timeDelta;
            }

            journeyStop.UpdatedAt = DateTime.UtcNow;
            _context.Entry(journeyStop).State = EntityState.Modified;

            _logger.LogInformation($"Updated JourneyStop {journeyStop.Id} with {timeDelta.TotalMinutes:F1} min delta: " +
                $"NewETA={journeyStop.EstimatedArrivalTime}, NewETD={journeyStop.EstimatedDepartureTime}");
        }

        _logger.LogInformation($"Applied {timeDelta.TotalMinutes:F1} minute delta to {pendingStops.Count} pending stops (no Google API call).");

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("=== OPTIMIZE JOURNEY FOR DEVIATION END ===");

        // Journey'yi tüm ilişkileriyle tekrar yükle
        journey = await _context.Journeys
            .Include(x => x.Route)
                .ThenInclude(r => r.Stops)
                    .ThenInclude(s => s.Customer)
            .Include(x => x.Route)
                .ThenInclude(r => r.Driver)
            .Include(x => x.Route)
                .ThenInclude(r => r.Vehicle)
            .Include(x => x.Route)
                .ThenInclude(r => r.Depot)
            .Include(x => x.Stops)
                .ThenInclude(s => s.RouteStop)
                    .ThenInclude(rs => rs!.Customer)
            .Include(x => x.Driver)
            .Include(x => x.StartDetails)
            .Include(x => x.EndDetails)
            .Include(x => x.Statuses)
            .FirstOrDefaultAsync(x => x.Id == request.JourneyId, cancellationToken);

        return new JourneyResponse(journey!);
    }
}