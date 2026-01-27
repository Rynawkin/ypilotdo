namespace Monolith.WebAPI.External.Google.Models;

public class GoogleDirectionsResponseBounds
{
    public GoogleDirectionsResponseNortheast northeast { get; set; }
    public GoogleDirectionsResponseSouthwest southwest { get; set; }
}

public class GoogleDirectionsResponseDistance
{
    public string text { get; set; }
    public int value { get; set; }
}

public class GoogleDirectionsResponseDuration
{
    public string text { get; set; }
    public int value { get; set; }
}

public class GoogleDirectionsResponseEndLocation
{
    public double lat { get; set; }
    public double lng { get; set; }
}

public class GoogleDirectionsResponseGeocodedWaypoint
{
    public string geocoder_status { get; set; }
    public string place_id { get; set; }
    public List<string> types { get; set; }
}

public class GoogleDirectionsResponseLeg
{
    public GoogleDirectionsResponseDistance distance { get; set; }
    public GoogleDirectionsResponseDuration duration { get; set; }
    public string end_address { get; set; }
    public GoogleDirectionsResponseEndLocation end_location { get; set; }
    public string start_address { get; set; }
    public GoogleDirectionsResponseStartLocation start_location { get; set; }
    public List<GoogleDirectionsResponseStep> steps { get; set; }
    public List<object> traffic_speed_entry { get; set; }
    public List<object> via_waypoint { get; set; }
}

public class GoogleDirectionsResponseNortheast
{
    public double lat { get; set; }
    public double lng { get; set; }
}

public class GoogleDirectionsResponseOverviewPolyline
{
    public string points { get; set; }
}

public class GoogleDirectionsResponsePolyline
{
    public string points { get; set; }
}

public class GoogleDirectionsResponse
{
    public List<GoogleDirectionsResponseGeocodedWaypoint> geocoded_waypoints { get; set; }
    public List<GoogleDirectionsResponseRoute> routes { get; set; }
    public string status { get; set; }
}

public class GoogleDirectionsResponseRoute
{
    public GoogleDirectionsResponseBounds bounds { get; set; }
    public string copyrights { get; set; }
    public List<GoogleDirectionsResponseLeg> legs { get; set; }
    public GoogleDirectionsResponseOverviewPolyline overview_polyline { get; set; }
    public string summary { get; set; }
    public List<object> warnings { get; set; }
    public List<int> waypoint_order { get; set; }
}

public class GoogleDirectionsResponseSouthwest
{
    public double lat { get; set; }
    public double lng { get; set; }
}

public class GoogleDirectionsResponseStartLocation
{
    public double lat { get; set; }
    public double lng { get; set; }
}

public class GoogleDirectionsResponseStep
{
    public GoogleDirectionsResponseDistance distance { get; set; }
    public GoogleDirectionsResponseDuration duration { get; set; }
    public GoogleDirectionsResponseEndLocation end_location { get; set; }
    public string html_instructions { get; set; }
    public GoogleDirectionsResponsePolyline polyline { get; set; }
    public GoogleDirectionsResponseStartLocation start_location { get; set; }
    public string travel_mode { get; set; }
    public string maneuver { get; set; }
}