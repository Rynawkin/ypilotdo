using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Responses.Workspace;

public class SavedLocationResponse(SavedLocation savedLocation)
{
    public int Id => savedLocation.Id;
    public string Name => savedLocation.Name;
    public string Address => savedLocation.Address;
    public double Latitude => savedLocation.Latitude;
    public double Longitude => savedLocation.Longitude;
}