using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Monolith.WebAPI.Data;

namespace Monolith.WebAPI.Infrastructure;

public class SubscriptionControllerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SubscriptionControllerMiddleware> _logger;

    public SubscriptionControllerMiddleware(
        RequestDelegate next, 
        IMemoryCache cache,
        ILogger<SubscriptionControllerMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext appDbContext)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        // Tüm /api/me endpoint'lerini bypass et
        if (path.StartsWith("/api/me"))
        {
            await _next(context);
            return;
        }
        
        // SignalR hub'ları için bypass
        if (path.StartsWith("/hubs"))
        {
            await _next(context);
            return;
        }

        // Sadece /workspace/ endpoint'leri için kontrol yap
        if (!path.Contains("/workspace/"))
        {
            await _next(context);
            return;
        }

        var authorizationHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
        {
            var token = authorizationHeader["Bearer ".Length..].Trim();

            if (!string.IsNullOrEmpty(token))
            {
                var handler = new JwtSecurityTokenHandler();

                if (handler.ReadToken(token) is JwtSecurityToken jwtToken)
                {
                    // DEBUG: Tüm claim'leri logla
                    _logger.LogInformation($"[Middleware] All claims in token:");
                    foreach (var claim in jwtToken.Claims)
                    {
                        _logger.LogInformation($"  - Type: {claim.Type}, Value: {claim.Value}");
                    }
                    
                    // WorkspaceId'yi hem büyük hem küçük harfle ara
                    var wsClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "WorkspaceId")?.Value
                               ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "workspaceId")?.Value;
                    
                    // Eğer claim yoksa header'dan almayı dene (FALLBACK)
                    if (string.IsNullOrWhiteSpace(wsClaim))
                    {
                        if (context.Request.Headers.TryGetValue("X-Workspace-Id", out var wsHeader))
                        {
                            wsClaim = wsHeader.FirstOrDefault();
                            _logger.LogWarning($"[Middleware] WorkspaceId not found in token, using header value: {wsClaim}");
                        }
                        else if (context.Request.Headers.TryGetValue("X-WorkspaceId", out var wsHeader2))
                        {
                            wsClaim = wsHeader2.FirstOrDefault();
                            _logger.LogWarning($"[Middleware] WorkspaceId not found in token, using X-WorkspaceId header: {wsClaim}");
                        }
                    }
                    
                    _logger.LogInformation($"[Middleware] Found WorkspaceId: {wsClaim}");
                    
                    if (!int.TryParse(wsClaim, out var workspaceId))
                    {
                        _logger.LogError("[Middleware] WorkspaceId not found in token or headers!");
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        await context.Response.WriteAsync("Workspace is not found.");
                        return;
                    }
                    
                    // Database'de workspace var mı kontrol et
                    var workspaceExists = await appDbContext.Workspaces
                        .AnyAsync(w => w.Id == workspaceId);
                    
                    _logger.LogInformation($"[Middleware] Workspace {workspaceId} exists: {workspaceExists}");
                    
                    if (!workspaceExists)
                    {
                        _logger.LogError($"[Middleware] Workspace with ID {workspaceId} does not exist in database!");
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        await context.Response.WriteAsync("Workspace is not found.");
                        return;
                    }
                    
                    var cacheKey = CacheKeys.WorkspaceActive(workspaceId);

                    if (!_cache.TryGetValue(cacheKey, out bool isActive))
                    {
                        isActive = await appDbContext.Workspaces.AsNoTracking()
                            .Where(s => s.Id == workspaceId)
                            .Select(x => x.Active)
                            .FirstOrDefaultAsync();

                        // BUGFIX: Must specify Size when SizeLimit is set on MemoryCache
                        var cacheOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                            .SetSize(1);
                        _cache.Set(cacheKey, isActive, cacheOptions);
                    }

                    _logger.LogInformation($"[Middleware] Workspace {workspaceId} active status: {isActive}");

                    if (!isActive)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("Workspace is not active. Please contact the administrator.");
                        return;
                    }
                }
            }
        }

        await _next(context);
    }
}