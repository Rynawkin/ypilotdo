using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Responses.Workspace;

public class DepotResponse
{
    // Boş constructor
    public DepotResponse() { }
    
    // Entity'den oluşturma constructor'ı
    public DepotResponse(Depot depot)
    {
        if (depot == null) return;
        
        Id = depot.Id;
        Name = depot.Name;
        Address = depot.Address;
        Latitude = depot.Latitude;
        Longitude = depot.Longitude;
        IsDefault = depot.IsDefault;
        WorkingHours = depot.WorkingHours;
        CreatedAt = depot.CreatedAt;
        UpdatedAt = depot.UpdatedAt ?? depot.CreatedAt; // Nullable handling
    }
    
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsDefault { get; set; }
    public Dictionary<string, WorkingHourDto> WorkingHours { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class WorkingHourResponse
{
    public string Open { get; set; }
    public string Close { get; set; }
}