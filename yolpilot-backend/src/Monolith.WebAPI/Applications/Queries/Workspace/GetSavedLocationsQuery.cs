using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Extensions;
using Monolith.WebAPI.Responses.Workspace;

namespace Monolith.WebAPI.Applications.Queries.Workspace;

public record GetSavedLocationsQuery(int WorkspaceId, string Text = null) : IRequest<IEnumerable<SavedLocationResponse>>;

public class GetSavedLocationsQueryHandler(AppDbContext context, IMemoryCache memoryCache)
    : IRequestHandler<GetSavedLocationsQuery, IEnumerable<SavedLocationResponse>>
{
    public async Task<IEnumerable<SavedLocationResponse>> Handle(GetSavedLocationsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.SavedLocationsByWorkspaceId(request.WorkspaceId);
        if (!memoryCache.TryGetValue(cacheKey, out List<SavedLocation> savedLocations))
        {
            savedLocations = await context.SavedLocations
                .Where(x => x.WorkspaceId == request.WorkspaceId)
                .ToListAsync(cancellationToken);

            memoryCache.Set(cacheKey, savedLocations, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                Size = 1 // BUGFIX: Must specify Size when SizeLimit is set
            });
        }
        
        //querying
        if (string.IsNullOrEmpty(request.Text) is false)
        {
            var text = request.Text.NormalizeText();

            savedLocations = savedLocations.Where(x =>
                x.Name.NormalizeText().Contains(text, StringComparison.OrdinalIgnoreCase) ||
                x.Address.NormalizeText().Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        
        return savedLocations.Select(x => new SavedLocationResponse(x));
    }
}