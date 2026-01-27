namespace Monolith.WebAPI.External.Google.Models;

public class GoogleDistanceResponseDistance
{
    public string text { get; set; }
    public int value { get; set; }
}

public class GoogleDistanceResponseDuration
{
    public string text { get; set; }
    public int value { get; set; }
}

public class GoogleDistanceResponseElement
{
    public GoogleDistanceResponseDistance distance { get; set; }
    public GoogleDistanceResponseDuration duration { get; set; }
    public string status { get; set; }
}

public class GoogleDistanceResponse
{
    public List<string> destination_addresses { get; set; }
    public List<string> origin_addresses { get; set; }
    public List<GoogleDistanceResponseRow> rows { get; set; }
    public string status { get; set; }
}

public class GoogleDistanceResponseRow
{
    public List<GoogleDistanceResponseElement> elements { get; set; }
}