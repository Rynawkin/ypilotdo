namespace Monolith.WebAPI.Responses;

public class SearchLocation(string name, string address, double latitude, double longitude)
{
    public string Id  => Guid.NewGuid().ToString();
    public string Name { get; private set; } = name;
    public string Address { get; private set; } = address;
    public double Latitude { get; private set; } = latitude;
    public double Longitude { get; private set; } = longitude;
}

public record SearchLocationResponse(List<SearchLocation> Saveds, List<SearchLocation> Places);