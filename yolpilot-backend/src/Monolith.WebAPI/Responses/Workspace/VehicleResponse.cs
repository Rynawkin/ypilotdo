namespace Monolith.WebAPI.Responses.Workspace;

public class VehicleResponse
{
    public int Id { get; set; } // Frontend'de string bekliyor, converter ile halledeceğiz
    public string PlateNumber { get; set; }
    public string Type { get; set; } // car, van, truck, motorcycle
    public string Brand { get; set; }
    public string Model { get; set; }
    public int Year { get; set; }
    public int Capacity { get; set; }
    public string Status { get; set; } // active, maintenance, inactive
    public string FuelType { get; set; } // gasoline, diesel, electric, hybrid
    public int? CurrentKm { get; set; } // ✅ YENİ - Araç kilometresi
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}