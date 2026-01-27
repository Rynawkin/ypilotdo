namespace Monolith.WebAPI.External.RouteXL.Models;

public class RouteXlResponseRouteStop
{
    public string Name { get; set; }
    public int Arrival { get; set; }
    public double Distance { get; set; }
}

public class RouteXlResponseRoute
{
    public Dictionary<string, RouteXlResponseRouteStop> Stops { get; set; }
}

public class RouteXlResponse
{
    public string Id { get; set; }
    public int Count { get; set; }
    public bool Feasible { get; set; }
    public Dictionary<string, RouteXlResponseRouteStop> Route { get; set; }
}