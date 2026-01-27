namespace Monolith.WebAPI.External.RouteXL.Models;

public class RouteXlRequestLocation
{
    public string address { get; set; }
    public string lat { get; set; }
    public string lng { get; set; }
    public int servicetime { get; set; }

    public RouteXlRequestLocationRestrictions restrictions { get; set; } = null;
}

public class RouteXlRequestLocationRestrictions
{
    public int? ready { get; set; } // Earliest time the location may be visited (minutes, greater than 0)
    public int? due { get; set; } // Latest time the location must be visited (minutes, greater than 0)
    public int? before { get; set; } // Index number of the location that is the delivery point for this location
    public int? after { get; set; } // Index number of the location that is the pickup point for this location
}