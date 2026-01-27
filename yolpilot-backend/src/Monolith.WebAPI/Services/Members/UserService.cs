using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Monolith.WebAPI.Applications.Commands.Members;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Workspace;

namespace Monolith.WebAPI.Services.Members;

public interface IUserService
{
    Task<ApplicationUser> GetUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<ApplicationUser> GetUserAsync(Guid userId, int workspaceId, CancellationToken cancellationToken);

    Task<Depot> UpdateDepotAsync(ApplicationUser willBeUpdatedUser, int? depotId, int workspaceId, CancellationToken cancellationToken);
    Task<ApplicationUser> UpdateRoleAsync(Guid updaterUserId, ApplicationUser willBeUpdatedUser, MemberRole[] roles, CancellationToken cancellationToken);
}

public class UserService(AppDbContext context, IMemoryCache memoryCache, IWorkspaceService workspaceService) : IUserService
{
    public async Task<ApplicationUser> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.UserById(userId);
        if (memoryCache.TryGetValue(cacheKey, out ApplicationUser user))
            return user;

        user = await context.Users
            .Include(x => x.Workspace)
            .Include(x => x.Depot)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is not null)
            memoryCache.Set(cacheKey, user, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                Size = 1 // BUGFIX: Must specify Size when SizeLimit is set
            });

        return user;
    }

    public async Task<ApplicationUser> GetUserAsync(Guid userId, int workspaceId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.UserById(userId);
        if (memoryCache.TryGetValue(cacheKey, out ApplicationUser user))
            return user;

        user = await context.Users.AsNoTracking()
            .Include(x => x.Workspace)
            .Include(x => x.Depot)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId && x.WorkspaceId == workspaceId, cancellationToken);

        if (user is not null)
            memoryCache.Set(cacheKey, user, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                Size = 1 // BUGFIX: Must specify Size when SizeLimit is set
            });

        return user;
    }

    public async Task<Depot> UpdateDepotAsync(ApplicationUser willBeUpdatedUser, int? depotId, int workspaceId, CancellationToken cancellationToken)
    {
        if (depotId.HasValue is false)
            return null;

        var depot = await context.Depots
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == depotId && x.WorkspaceId == workspaceId, cancellationToken);
        if (depot == null)
            throw new ApiException("Depot not found", 404);

        willBeUpdatedUser.SetDepot(depot);

        return depot;
    }

    public async Task<ApplicationUser> UpdateRoleAsync(Guid updaterUserId, ApplicationUser willBeUpdatedUser, MemberRole[] roles, CancellationToken cancellationToken)
    {
        await UpdateAdminRole(updaterUserId, willBeUpdatedUser, roles, cancellationToken);
        await UpdateDriverRole(willBeUpdatedUser, roles, cancellationToken);

        willBeUpdatedUser.SetDispatcher(roles.Contains(MemberRole.Dispatcher));
        return willBeUpdatedUser;
    }

    private async Task UpdateDriverRole(ApplicationUser willBeUpdatedUser, IEnumerable<MemberRole> roles, CancellationToken cancellationToken)
    {
        var driverRole = roles.Contains(MemberRole.Driver);
        if (driverRole)
            await workspaceService.ThrowExceptionIfReachedToMaxDriverCountAsync(willBeUpdatedUser.WorkspaceId, cancellationToken);
        
        willBeUpdatedUser.SetDriver(driverRole);
    }

    private async Task UpdateAdminRole(Guid updaterUserId, ApplicationUser willBeUpdatedUser, IEnumerable<MemberRole> roles, CancellationToken cancellationToken)
    {
        var adminRole = roles.Contains(MemberRole.Admin);
        if (adminRole is false)
        {
            var workspaceAdminCount = await context.Users
                .AsNoTracking()
                .CountAsync(x => x.WorkspaceId == willBeUpdatedUser.WorkspaceId && x.IsAdmin, cancellationToken);
            
            if (workspaceAdminCount == 1)
                throw new ApiException("You cannot remove the last admin", 400);
        }
        
        if(willBeUpdatedUser.Id != updaterUserId)
            willBeUpdatedUser.SetAdmin(adminRole);
    }
}