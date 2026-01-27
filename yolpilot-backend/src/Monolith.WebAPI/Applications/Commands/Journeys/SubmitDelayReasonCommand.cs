using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class SubmitDelayReasonCommand : BaseAuthenticatedCommand<bool>
{
    [JsonIgnore] public override bool RequiresDriver => true;
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;
    [JsonIgnore] public int JourneyId { get; set; }
    [JsonIgnore] public int StopId { get; set; }

    public DelayReasonCategory DelayReasonCategory { get; set; }
    public string DelayReason { get; set; }
}

public class SubmitDelayReasonCommandValidator : AbstractValidator<SubmitDelayReasonCommand>
{
    public SubmitDelayReasonCommandValidator()
    {
        RuleFor(x => x.DelayReasonCategory).IsInEnum();
        RuleFor(x => x.DelayReason).MaximumLength(500);
    }
}

public class SubmitDelayReasonCommandHandler : BaseAuthenticatedCommandHandler<SubmitDelayReasonCommand, bool>
{
    private readonly AppDbContext _context;
    private readonly ILogger<SubmitDelayReasonCommandHandler> _logger;

    public SubmitDelayReasonCommandHandler(
        AppDbContext context,
        IUserService userService,
        ILogger<SubmitDelayReasonCommandHandler> logger)
        : base(userService)
    {
        _context = context;
        _logger = logger;
    }

    protected override async Task<bool> HandleCommand(SubmitDelayReasonCommand request, CancellationToken cancellationToken)
    {
        var stop = await _context.JourneyStops
            .Include(s => s.Journey)
            .FirstOrDefaultAsync(s =>
                s.Id == request.StopId &&
                s.JourneyId == request.JourneyId &&
                s.Journey.WorkspaceId == User.WorkspaceId,
                cancellationToken);

        if (stop == null)
        {
            throw new ApiException($"Stop {request.StopId} not found in journey {request.JourneyId}", 404);
        }

        // Türkiye timezone
        var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

        // Önceki durakların kümülatif gecikmesini hesapla
        var previousStops = await _context.JourneyStops
            .Where(s => s.JourneyId == request.JourneyId && s.Order < stop.Order && s.CheckInTime.HasValue)
            .OrderByDescending(s => s.Order)
            .ToListAsync(cancellationToken);

        int previousCumulativeDelay = 0;
        if (previousStops.Any())
        {
            var previousStop = previousStops.First();
            previousCumulativeDelay = previousStop.CumulativeDelay;
        }

        // Mevcut durağın gerçek gecikmesini hesapla
        var actualDelay = stop.CalculateActualDelay(turkeyTimeZone);

        // Yeni gecikmeyi hesapla
        var newDelay = actualDelay - previousCumulativeDelay;

        // Kümülatif gecikmeyi hesapla
        var cumulativeDelay = actualDelay;

        // Gecikme sebebini kaydet
        stop.SetDelayReason(request.DelayReasonCategory, request.DelayReason, newDelay, cumulativeDelay);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Delay reason submitted for journey {JourneyId}, stop {StopId}: {Category} - {Reason} (New: {NewDelay}min, Cumulative: {CumulativeDelay}min)",
            request.JourneyId, request.StopId, request.DelayReasonCategory, request.DelayReason, newDelay, cumulativeDelay);

        return true;
    }
}
