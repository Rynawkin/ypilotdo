using System.Threading.Tasks;
using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Services.Feedback
{
    public interface IFeedbackService
    {
        Task<CustomerFeedback> SubmitFeedbackAsync(string token, int overallRating, int? deliverySpeed, int? driverBehavior, int? packageCondition, string comments, string ipAddress, string userAgent);
        Task<CustomerFeedback> GetFeedbackByTokenAsync(string token);
        Task<List<CustomerFeedback>> GetWorkspaceFeedbackAsync(int workspaceId, DateTime? startDate, DateTime? endDate);
        Task<object> GetFeedbackStatsAsync(int workspaceId, DateTime? startDate, DateTime? endDate);
        string GenerateFeedbackToken(int journeyId, int stopId, int customerId);
        FeedbackTokenData ValidateAndExtractToken(string token);
    }

    public class FeedbackTokenData
    {
        public int WorkspaceId { get; set; }
        public int JourneyId { get; set; }
        public int StopId { get; set; }
        public int CustomerId { get; set; }
    }
}