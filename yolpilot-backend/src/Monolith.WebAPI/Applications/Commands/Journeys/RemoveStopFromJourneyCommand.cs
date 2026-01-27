using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Hubs;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class RemoveStopFromJourneyCommand : BaseAuthenticatedCommand<JourneyResponse>
{
    [JsonIgnore] public override bool RequiresDispatcher => true; // Sadece dispatcher/admin silebilir
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;

    [JsonIgnore] public int JourneyId { get; init; }
    [JsonIgnore] public int StopId { get; init; }
}

public class RemoveStopFromJourneyCommandValidator : AbstractValidator<RemoveStopFromJourneyCommand>
{
    public RemoveStopFromJourneyCommandValidator()
    {
        RuleFor(x => x.JourneyId).GreaterThan(0);
        RuleFor(x => x.StopId).GreaterThan(0);
    }
}

public class RemoveStopFromJourneyCommandHandler : BaseAuthenticatedCommandHandler<RemoveStopFromJourneyCommand, JourneyResponse>
{
    private readonly AppDbContext _context;
    private readonly IHubContext<JourneyHub> _journeyHub;
    private readonly ILogger<RemoveStopFromJourneyCommandHandler> _logger;

    public RemoveStopFromJourneyCommandHandler(
        AppDbContext context,
        IUserService userService,
        IHubContext<JourneyHub> journeyHub,
        ILogger<RemoveStopFromJourneyCommandHandler> logger) : base(userService)
    {
        _context = context;
        _journeyHub = journeyHub;
        _logger = logger;
    }

    protected override async Task<JourneyResponse> HandleCommand(RemoveStopFromJourneyCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"[REMOVE_STOP] Removing stop #{request.StopId} from journey #{request.JourneyId}");

        // Journey'i yükle
        var journey = await _context.Journeys
            .Include(j => j.Route)
                .ThenInclude(r => r.Workspace)
            .Include(j => j.Route)
                .ThenInclude(r => r.Stops)
                    .ThenInclude(s => s.Customer)
            .Include(j => j.Stops.OrderBy(s => s.Order))
                .ThenInclude(s => s.RouteStop)
                    .ThenInclude(rs => rs.Customer)
            .Include(j => j.Driver)
            .Include(j => j.Vehicle)
            .Include(j => j.StartDetails)
            .Include(j => j.EndDetails)
            .FirstOrDefaultAsync(x => x.Id == request.JourneyId && x.WorkspaceId == User.WorkspaceId, cancellationToken);

        if (journey is null)
            throw new ApiException("Sefer bulunamadı", 404);

        // Tamamlanmış veya iptal edilmiş seferlerde durak silinemez
        if (journey.IsCompleted || journey.IsCancelled)
            throw new ApiException("Tamamlanmış veya iptal edilmiş seferlerde durak silinemez", 400);

        // Durağı bul
        var stop = journey.Stops.FirstOrDefault(s => s.Id == request.StopId);
        if (stop is null)
            throw new ApiException("Durak bulunamadı", 404);

        // Tamamlanmış veya başarısız duraklar silinemez
        if (stop.Status != JourneyStopStatus.Pending)
            throw new ApiException("Sadece bekleyen (pending) duraklar silinebilir", 400);

        // Son durak (depo) silinemez
        var maxOrder = journey.Stops.Max(s => s.Order);
        if (stop.Order == maxOrder)
            throw new ApiException("Son durak (depo) silinemez", 400);

        _logger.LogInformation($"[REMOVE_STOP] Removing stop with order {stop.Order}");

        // Durağı sil
        _context.JourneyStops.Remove(stop);

        // Sonraki durakların order'larını güncelle
        var subsequentStops = journey.Stops.Where(s => s.Order > stop.Order).OrderBy(s => s.Order).ToList();
        foreach (var subsequentStop in subsequentStops)
        {
            subsequentStop.Order -= 1;
        }

        // Optimizasyon bayrağını set et
        journey.NeedsReoptimization = true;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation($"[REMOVE_STOP] Stop removed successfully. {subsequentStops.Count} stops reordered.");

        // SignalR ile bildirim gönder
        try
        {
            await _journeyHub.Clients.Group($"journey_{journey.Id}").SendAsync(
                "StopRemoved",
                new
                {
                    JourneyId = journey.Id,
                    Message = "Durak kaldırıldı. Optimizasyon gerekiyor.",
                    NeedsReoptimization = true,
                    RemovedStopId = request.StopId
                },
                cancellationToken
            );

            _logger.LogInformation($"[REMOVE_STOP] SignalR notification sent to journey #{journey.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[REMOVE_STOP] Failed to send SignalR notification for journey #{journey.Id}");
        }

        // Journey'i tekrar yükle (ilişkilerle birlikte)
        journey = await _context.Journeys
            .Include(j => j.Route)
                .ThenInclude(r => r.Workspace)
            .Include(j => j.Route)
                .ThenInclude(r => r.Stops)
                    .ThenInclude(s => s.Customer)
            .Include(j => j.Stops.OrderBy(s => s.Order))
                .ThenInclude(s => s.RouteStop)
                    .ThenInclude(rs => rs.Customer)
            .Include(j => j.Driver)
            .Include(j => j.Vehicle)
            .Include(j => j.StartDetails)
            .Include(j => j.EndDetails)
            .Include(j => j.Statuses)
            .FirstOrDefaultAsync(x => x.Id == request.JourneyId, cancellationToken);

        return new JourneyResponse(journey);
    }
}
