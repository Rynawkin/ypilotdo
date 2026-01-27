using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Journeys;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Journeys;

public class GetJourneyStatusesQuery : BaseAuthenticatedCommand<IEnumerable<JourneyStatusResponse>>
{
    public int JourneyId { get; set; }
    
    public override bool RequiresDriver => false;
}

public class GetJourneyStatusesQueryHandler : BaseAuthenticatedCommandHandler<GetJourneyStatusesQuery, IEnumerable<JourneyStatusResponse>>
{
    private readonly AppDbContext _context;

    public GetJourneyStatusesQueryHandler(AppDbContext context, IUserService userService)
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<IEnumerable<JourneyStatusResponse>> HandleCommand(GetJourneyStatusesQuery request, CancellationToken cancellationToken)
    {
        var journeyQuery = _context.Journeys
            .AsNoTracking()
            .Include(j => j.Driver)  // ✅ DÜZELTME: Driver include edildi
            .Include(j => j.Route)
            .Where(x => x.Id == request.JourneyId && x.Route.WorkspaceId == User.WorkspaceId);

        // ✅ ROL BAZLI FİLTRELEME
        // Driver ise sadece kendisine atanan journey'nin statuslarını görebilir
        if (User.IsDriver && !User.IsDispatcher && !User.IsAdmin && !User.IsSuperAdmin)
        {
            // ✅ DÜZELTME: Driver.UserId artık mevcut
            journeyQuery = journeyQuery.Where(j => j.Driver != null && j.Driver.UserId == User.Id);
        }

        var journey = await journeyQuery.FirstOrDefaultAsync(cancellationToken);
        
        if (journey == null)
            throw new ApiException("Journey not found or access denied", 404);

        var journeyStatuses = await _context.JourneyStatuses
            .AsNoTracking()
            .Where(x => x.JourneyId == request.JourneyId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return journeyStatuses.Select(x => new JourneyStatusResponse(x));
    }
}