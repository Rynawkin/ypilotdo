namespace Monolith.WebAPI.Responses.Workspace;

public class WorkspaceResponse(Data.Workspace.Workspace workspace)
{
    public int Id => workspace.Id;
    public string Name => workspace.Name;
    public string PhoneNumber => workspace.PhoneNumber;
    public string Email => workspace.Email;
    public string DistanceUnit => workspace.DistanceUnit;
    public string Currency => workspace.Currency;
    public string TimeZone => workspace.TimeZone;
    public double? CostPerKm => workspace.CostPerKm;
    public double? CostPerHour => workspace.CostPerHour;

    public TimeSpan DefaultServiceTime => workspace.DefaultServiceTime;

    public IEnumerable<DepotResponse> Depots => workspace?.Depots?.Select(d => new DepotResponse(d)).ToList();
}