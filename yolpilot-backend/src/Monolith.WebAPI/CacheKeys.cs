namespace Monolith.WebAPI;

public static class CacheKeys
{
    public static string UserById(Guid userId) => $"User_{userId.ToString()}";
    public static string SavedLocationsByWorkspaceId(int workspaceId) => $"GetSavedLocationsQuery_{workspaceId}";
    public static string GooglePlacesByQuery(string query) => $"GooglePlacesResponse_{query}";
    public static string WorkspaceActive(int workspaceId) => $"WorkspaceActive_{workspaceId}";
}