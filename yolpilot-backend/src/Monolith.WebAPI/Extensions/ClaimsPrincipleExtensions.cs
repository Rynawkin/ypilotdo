using System.Security.Claims;

namespace Monolith.WebAPI.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUsername(this ClaimsPrincipal user) => 
        user.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

    public static Guid GetId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    // GetUserId metodu - GetId ile aynı işlevi görür, uyumluluk için eklendi
    public static Guid GetUserId(this ClaimsPrincipal user) => GetId(user);

    public static int GetWorkspaceId(this ClaimsPrincipal user)
    {
        // Önce büyük harfle dene
        var workspaceId = user.FindFirst("WorkspaceId")?.Value;
        
        // Bulunamazsa küçük harfle dene
        if (string.IsNullOrEmpty(workspaceId))
        {
            workspaceId = user.FindFirst("workspaceId")?.Value;
        }
        
        // Hala bulunamazsa workspaceID dene
        if (string.IsNullOrEmpty(workspaceId))
        {
            workspaceId = user.FindFirst("workspaceID")?.Value;
        }
        
        // Debug için
        if (string.IsNullOrEmpty(workspaceId))
        {
            Console.WriteLine($"WorkspaceId claim not found. Available claims: {string.Join(", ", user.Claims.Select(c => c.Type))}");
            return 0;
        }
        
        if (int.TryParse(workspaceId, out var id))
        {
            return id;
        }
        
        Console.WriteLine($"Failed to parse WorkspaceId: {workspaceId}");
        return 0;
    }

    // Ek yardımcı metodlar
    public static string GetUserEmail(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Email)?.Value 
        ?? user.FindFirst("email")?.Value
        ?? string.Empty;

    public static bool IsDriver(this ClaimsPrincipal user)
    {
        var isDriverClaim = user.FindFirst("IsDriver")?.Value;
        return bool.TryParse(isDriverClaim, out var isDriver) && isDriver;
    }

    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        var isAdminClaim = user.FindFirst("IsAdmin")?.Value;
        return bool.TryParse(isAdminClaim, out var isAdmin) && isAdmin;
    }

    public static bool IsSuperAdmin(this ClaimsPrincipal user)
    {
        var isSuperAdminClaim = user.FindFirst("IsSuperAdmin")?.Value;
        return bool.TryParse(isSuperAdminClaim, out var isSuperAdmin) && isSuperAdmin;
    }
}