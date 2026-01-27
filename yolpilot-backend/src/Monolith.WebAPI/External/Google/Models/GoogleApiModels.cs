using System.Text.Json.Serialization;

namespace Monolith.WebAPI.External.Google.Models;

// Place Search Response
public class PlaceSearchResponse
{
    [JsonPropertyName("results")]
    public List<PlaceResult> Results { get; set; } = new();
    
    [JsonPropertyName("status")]
    public string Status { get; set; }
    
    [JsonPropertyName("error_message")]
    public string ErrorMessage { get; set; }
}

public class PlaceResult
{
    [JsonPropertyName("place_id")]
    public string PlaceId { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("formatted_address")]
    public string FormattedAddress { get; set; }
    
    [JsonPropertyName("geometry")]
    public Geometry Geometry { get; set; }
    
    [JsonPropertyName("rating")]
    public double? Rating { get; set; }
    
    [JsonPropertyName("types")]
    public List<string> Types { get; set; }
}

// Place Details Response
public class PlaceDetailsResponse
{
    [JsonPropertyName("result")]
    public PlaceDetails Result { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; }
}

public class PlaceDetails
{
    [JsonPropertyName("place_id")]
    public string PlaceId { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("formatted_address")]
    public string FormattedAddress { get; set; }
    
    [JsonPropertyName("formatted_phone_number")]
    public string FormattedPhoneNumber { get; set; }
    
    [JsonPropertyName("website")]
    public string Website { get; set; }
    
    [JsonPropertyName("geometry")]
    public Geometry Geometry { get; set; }
    
    [JsonPropertyName("opening_hours")]
    public OpeningHours OpeningHours { get; set; }
    
    [JsonPropertyName("rating")]
    public double? Rating { get; set; }
}

// Geocoding Response
public class GeocodingResponse
{
    [JsonPropertyName("results")]
    public List<GeocodingResult> Results { get; set; } = new();
    
    [JsonPropertyName("status")]
    public string Status { get; set; }
}

public class GeocodingResult
{
    [JsonPropertyName("formatted_address")]
    public string FormattedAddress { get; set; }
    
    [JsonPropertyName("geometry")]
    public Geometry Geometry { get; set; }
    
    [JsonPropertyName("place_id")]
    public string PlaceId { get; set; }
    
    [JsonPropertyName("types")]
    public List<string> Types { get; set; }
}

// Directions Response - UPDATED
public class DirectionsResponse
{
    [JsonPropertyName("routes")]
    public List<Route> Routes { get; set; } = new();
    
    [JsonPropertyName("status")]
    public string Status { get; set; }
    
    [JsonPropertyName("waypoint_order")]
    public List<int> WaypointOrder { get; set; }
    
    [JsonPropertyName("error_message")]
    public string ErrorMessage { get; set; } // ADDED
    
    [JsonPropertyName("geocoded_waypoints")]
    public List<GeocodedWaypoint> GeocodedWaypoints { get; set; } // ADDED
}

// ADDED - GeocodedWaypoint class
public class GeocodedWaypoint
{
    [JsonPropertyName("geocoder_status")]
    public string GeocoderStatus { get; set; }
    
    [JsonPropertyName("place_id")]
    public string PlaceId { get; set; }
    
    [JsonPropertyName("types")]
    public List<string> Types { get; set; }
}

public class Route
{
    [JsonPropertyName("legs")]
    public List<Leg> Legs { get; set; }
    
    [JsonPropertyName("overview_polyline")]
    public Polyline OverviewPolyline { get; set; }
    
    [JsonPropertyName("bounds")]
    public Bounds Bounds { get; set; }
    
    // ✅ ÖNEMLİ: waypoint_order eklendi
    [JsonPropertyName("waypoint_order")]
    public List<int> WaypointOrder { get; set; }
}

public class Leg
{
    [JsonPropertyName("distance")]
    public Distance Distance { get; set; }
    
    [JsonPropertyName("duration")]
    public Duration Duration { get; set; }
    
    [JsonPropertyName("start_address")]
    public string StartAddress { get; set; }
    
    [JsonPropertyName("end_address")]
    public string EndAddress { get; set; }
    
    [JsonPropertyName("start_location")]
    public Location StartLocation { get; set; }
    
    [JsonPropertyName("end_location")]
    public Location EndLocation { get; set; }
    
    [JsonPropertyName("steps")]
    public List<Step> Steps { get; set; }
}

// Distance Matrix Response
public class DistanceMatrixResponse
{
    [JsonPropertyName("rows")]
    public List<DistanceMatrixRow> Rows { get; set; } = new();
    
    [JsonPropertyName("origin_addresses")]
    public List<string> OriginAddresses { get; set; }
    
    [JsonPropertyName("destination_addresses")]
    public List<string> DestinationAddresses { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; }
}

public class DistanceMatrixRow
{
    [JsonPropertyName("elements")]
    public List<DistanceMatrixElement> Elements { get; set; }
}

public class DistanceMatrixElement
{
    [JsonPropertyName("distance")]
    public Distance Distance { get; set; }
    
    [JsonPropertyName("duration")]
    public Duration Duration { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; }
}

// Autocomplete Response
public class AutocompleteResponse
{
    [JsonPropertyName("predictions")]
    public List<AutocompletePrediction> Predictions { get; set; } = new();
    
    [JsonPropertyName("status")]
    public string Status { get; set; }
}

public class AutocompletePrediction
{
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("place_id")]
    public string PlaceId { get; set; }
    
    [JsonPropertyName("structured_formatting")]
    public StructuredFormatting StructuredFormatting { get; set; }
    
    [JsonPropertyName("types")]
    public List<string> Types { get; set; }
}

// Common Models
public class Geometry
{
    [JsonPropertyName("location")]
    public Location Location { get; set; }
    
    [JsonPropertyName("viewport")]
    public Viewport Viewport { get; set; }
}

public class Location
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }
    
    [JsonPropertyName("lng")]
    public double Lng { get; set; }
}

public class Viewport
{
    [JsonPropertyName("northeast")]
    public Location Northeast { get; set; }
    
    [JsonPropertyName("southwest")]
    public Location Southwest { get; set; }
}

public class Bounds
{
    [JsonPropertyName("northeast")]
    public Location Northeast { get; set; }
    
    [JsonPropertyName("southwest")]
    public Location Southwest { get; set; }
}

public class Distance
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
    
    [JsonPropertyName("value")]
    public int Value { get; set; }
}

public class Duration
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
    
    [JsonPropertyName("value")]
    public int Value { get; set; }
}

public class Polyline
{
    [JsonPropertyName("points")]
    public string Points { get; set; }
}

public class Step
{
    [JsonPropertyName("distance")]
    public Distance Distance { get; set; }
    
    [JsonPropertyName("duration")]
    public Duration Duration { get; set; }
    
    [JsonPropertyName("start_location")]
    public Location StartLocation { get; set; }
    
    [JsonPropertyName("end_location")]
    public Location EndLocation { get; set; }
    
    [JsonPropertyName("html_instructions")]
    public string HtmlInstructions { get; set; }
    
    [JsonPropertyName("polyline")]
    public Polyline Polyline { get; set; }
}

public class OpeningHours
{
    [JsonPropertyName("open_now")]
    public bool OpenNow { get; set; }
    
    [JsonPropertyName("weekday_text")]
    public List<string> WeekdayText { get; set; }
}

public class StructuredFormatting
{
    [JsonPropertyName("main_text")]
    public string MainText { get; set; }
    
    [JsonPropertyName("secondary_text")]
    public string SecondaryText { get; set; }
}