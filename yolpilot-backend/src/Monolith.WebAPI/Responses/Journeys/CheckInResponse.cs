namespace Monolith.WebAPI.Responses.Journeys;

public class CheckInResponse
{
    /// <summary>
    /// CheckIn başarılı oldu mu
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gecikme sebebi sorulsun mu (15+ dakika yeni gecikme varsa)
    /// </summary>
    public bool RequiresDelayReason { get; set; }

    /// <summary>
    /// Bu durağa özgü yeni gecikme (dakika)
    /// </summary>
    public int NewDelay { get; set; }

    /// <summary>
    /// Toplam kümülatif gecikme (dakika)
    /// </summary>
    public int CumulativeDelay { get; set; }

    /// <summary>
    /// Hata mesajı (varsa)
    /// </summary>
    public string Message { get; set; }
}
