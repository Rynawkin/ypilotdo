namespace Monolith.WebAPI.External.Google.Models;

public class GooglePlaceResponseDisplayName
{
    public string text { get; set; }
    public string languageCode { get; set; }
}

public class GooglePlaceResponseLocation
{
    public double latitude { get; set; }
    public double longitude { get; set; }
}

public class GooglePlaceResponsePlace
{
    public string formattedAddress { get; set; }
    public GooglePlaceResponseLocation location { get; set; }
    public GooglePlaceResponseDisplayName displayName { get; set; }
}

public class GooglePlaceResponse
{
    public List<GooglePlaceResponsePlace> places { get; set; }
}