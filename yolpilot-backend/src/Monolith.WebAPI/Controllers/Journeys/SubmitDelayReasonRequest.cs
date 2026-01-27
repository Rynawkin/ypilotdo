using Monolith.WebAPI.Data.Journeys;

namespace Monolith.WebAPI.Controllers.Journeys;

public class SubmitDelayReasonRequest
{
    public DelayReasonCategory DelayReasonCategory { get; set; }
    public string DelayReason { get; set; }
}
