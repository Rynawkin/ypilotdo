// src/Monolith.WebAPI/Responses/Journeys/JourneySummaryResponse.cs
namespace Monolith.WebAPI.Responses.Journeys;

public class JourneySummaryResponse
{
    public int TotalJourneys { get; set; }
    public int CompletedJourneys { get; set; }
    public int InProgressJourneys { get; set; }
    public int CancelledJourneys { get; set; }
    public int PlannedJourneys { get; set; }
    public decimal TotalDistance { get; set; }
    public decimal TotalDuration { get; set; }
    public decimal CompletionRate { get; set; }
    public bool IsDriverView { get; set; } // Driver kendi istatistiklerini mi görüyor
}