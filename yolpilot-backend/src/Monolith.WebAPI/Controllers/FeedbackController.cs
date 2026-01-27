using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Extensions;
using Monolith.WebAPI.Services.Feedback;

namespace Monolith.WebAPI.Controllers
{
    [ApiController]
    [Route("api")]
    public class FeedbackController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IFeedbackService _feedbackService;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(
            AppDbContext context,
            IFeedbackService feedbackService,
            ILogger<FeedbackController> logger)
        {
            _context = context;
            _feedbackService = feedbackService;
            _logger = logger;
        }

        /// <summary>
        /// Müşteri feedback formu bilgilerini getir
        /// </summary>
        [HttpGet("feedback/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFeedbackForm(string token)
        {
            try
            {
                // Önce token ile mevcut feedback kaydını ara
                var existingFeedback = await _context.CustomerFeedback
                    .Include(f => f.Customer)
                    .Include(f => f.JourneyStop)
                        .ThenInclude(js => js.Journey)
                            .ThenInclude(j => j.Driver)
                    .Include(f => f.Workspace)
                    .FirstOrDefaultAsync(f => f.FeedbackToken == token);

                // Eğer kayıt varsa ve doldurulmuşsa (stub değilse)
                if (existingFeedback != null && existingFeedback.Comments != "[STUB]")
                {
                    return Ok(new
                    {
                        alreadySubmitted = true,
                        submittedAt = existingFeedback.SubmittedAt,
                        rating = existingFeedback.OverallRating,
                        message = "Bu değerlendirme formu daha önce doldurulmuş."
                    });
                }

                // Token'dan bilgileri çıkar (journeyId, stopId, customerId)
                var tokenData = _feedbackService.ValidateAndExtractToken(token);
                if (tokenData == null)
                {
                    return NotFound(new { message = "Geçersiz veya süresi dolmuş feedback linki" });
                }

                // Journey ve müşteri bilgilerini getir
                var journeyStop = await _context.JourneyStops
                    .Include(js => js.Journey)
                        .ThenInclude(j => j.Driver)
                    .Include(js => js.Journey)
                        .ThenInclude(j => j.Workspace)
                    .Include(js => js.RouteStop)
                        .ThenInclude(rs => rs.Customer)
                    .FirstOrDefaultAsync(js => 
                        js.JourneyId == tokenData.JourneyId && 
                        js.Id == tokenData.StopId);

                if (journeyStop == null)
                {
                    return NotFound(new { message = "Teslimat bilgisi bulunamadı" });
                }

                var customer = journeyStop.RouteStop?.Customer;
                if (customer == null || customer.Id != tokenData.CustomerId)
                {
                    return NotFound(new { message = "Müşteri bilgisi bulunamadı" });
                }

                // Form bilgilerini döndür
                return Ok(new
                {
                    token = token,
                    customerName = customer.Name,
                    customerAddress = customer.Address,
                    driverName = journeyStop.Journey?.Driver?.Name,
                    deliveryDate = journeyStop.CheckOutTime ?? journeyStop.CheckInTime,
                    companyName = journeyStop.Journey?.Workspace?.Name,
                    companyLogo = journeyStop.Journey?.Workspace?.Settings?.Logo,
                    alreadySubmitted = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feedback form for token {Token}", token);
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }

        [HttpPost("feedback/submit")]
        [AllowAnonymous]
        public async Task<IActionResult> SubmitFeedback([FromBody] SubmitFeedbackRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Token))
                {
                    return BadRequest(new { message = "Token gerekli" });
                }

                // Token'dan bilgileri çıkar
                var tokenData = _feedbackService.ValidateAndExtractToken(request.Token);
                if (tokenData == null)
                {
                    return NotFound(new { message = "Geçersiz feedback linki" });
                }

                // Mevcut feedback kaydını kontrol et
                var existingFeedback = await _context.CustomerFeedback
                    .FirstOrDefaultAsync(f => f.FeedbackToken == request.Token);

                if (existingFeedback != null)
                {
                    // Daha önce doldurulmuş mu? (stub değilse)
                    if (existingFeedback.Comments != "[STUB]")
                    {
                        return BadRequest(new { message = "Bu form daha önce doldurulmuş" });
                    }

                    // Mevcut kaydı güncelle
                    existingFeedback.OverallRating = request.OverallRating;
                    existingFeedback.DeliverySpeedRating = request.DeliverySpeedRating;
                    existingFeedback.DriverBehaviorRating = request.DriverBehaviorRating;
                    existingFeedback.PackageConditionRating = request.PackageConditionRating;
                    existingFeedback.Comments = request.Comments;
                    existingFeedback.SubmitterName = request.SubmitterName;
                    existingFeedback.SubmitterEmail = request.SubmitterEmail;
                    existingFeedback.SubmitterPhone = request.SubmitterPhone;
                    existingFeedback.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    existingFeedback.UserAgent = Request.Headers["User-Agent"].ToString();
                    existingFeedback.SubmittedAt = DateTime.UtcNow;
                }
                else
                {
                    // Yeni feedback kaydı oluştur
                    var feedback = new CustomerFeedback
                    {
                        WorkspaceId = tokenData.WorkspaceId,
                        JourneyId = tokenData.JourneyId,
                        JourneyStopId = tokenData.StopId,
                        CustomerId = tokenData.CustomerId,
                        FeedbackToken = request.Token,
                        OverallRating = request.OverallRating,
                        DeliverySpeedRating = request.DeliverySpeedRating,
                        DriverBehaviorRating = request.DriverBehaviorRating,
                        PackageConditionRating = request.PackageConditionRating,
                        Comments = request.Comments,
                        SubmitterName = request.SubmitterName,
                        SubmitterEmail = request.SubmitterEmail,
                        SubmitterPhone = request.SubmitterPhone,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = Request.Headers["User-Agent"].ToString(),
                        SubmittedAt = DateTime.UtcNow
                    };
                    
                    _context.CustomerFeedback.Add(feedback);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Değerlendirmeniz için teşekkür ederiz!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting feedback");
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }

        /// <summary>
        /// Workspace feedback listesi
        /// </summary>
        [HttpGet("workspace/feedback")]
        [Authorize]
        public async Task<IActionResult> GetWorkspaceFeedback(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? page = 1,
            [FromQuery] int? pageSize = 20)
        {
            try
            {
                var workspaceId = User.GetWorkspaceId();
                
                var query = _context.CustomerFeedback
                    .Include(f => f.Customer)
                    .Include(f => f.JourneyStop)
                        .ThenInclude(js => js.Journey)
                            .ThenInclude(j => j.Driver)
                    .Where(f => f.WorkspaceId == workspaceId && f.Comments != "[STUB]");

                if (startDate.HasValue)
                    query = query.Where(f => f.SubmittedAt >= startDate.Value.Date);

                if (endDate.HasValue)
                    query = query.Where(f => f.SubmittedAt < endDate.Value.Date.AddDays(1)); // Günün sonuna kadar dahil et

                var totalCount = await query.CountAsync();
                
                var feedbacks = await query
                    .OrderByDescending(f => f.SubmittedAt)
                    .Skip((page.Value - 1) * pageSize.Value)
                    .Take(pageSize.Value)
                    .Select(f => new
                    {
                        f.Id,
                        f.OverallRating,
                        f.DeliverySpeedRating,
                        f.DriverBehaviorRating,
                        f.PackageConditionRating,
                        f.Comments,
                        f.SubmittedAt,
                        f.SubmitterName,
                        f.SubmitterEmail,
                        f.SubmitterPhone,
                        Customer = new
                        {
                            f.Customer.Id,
                            f.Customer.Name,
                            f.Customer.Address
                        },
                        Driver = f.JourneyStop.Journey.Driver != null ? new
                        {
                            f.JourneyStop.Journey.Driver.Id,
                            f.JourneyStop.Journey.Driver.Name
                        } : null,
                        Journey = new
                        {
                            f.JourneyStop.Journey.Id,
                            f.JourneyStop.Journey.Date
                        }
                    })
                    .ToListAsync();

                return Ok(new
                {
                    data = feedbacks,
                    totalCount,
                    page = page.Value,
                    pageSize = pageSize.Value,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize.Value)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workspace feedback");
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }

        /// <summary>
        /// Workspace feedback istatistikleri
        /// </summary>
        [HttpGet("workspace/feedback/stats")]
        [Authorize]
        public async Task<IActionResult> GetFeedbackStats(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var workspaceId = User.GetWorkspaceId();
                var stats = await _feedbackService.GetFeedbackStatsAsync(workspaceId, startDate, endDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feedback stats");
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }
    }

    public class SubmitFeedbackRequest
    {
        public string Token { get; set; }
        public int OverallRating { get; set; }
        public int? DeliverySpeedRating { get; set; }
        public int? DriverBehaviorRating { get; set; }
        public int? PackageConditionRating { get; set; }
        public string Comments { get; set; }

        [Required]
        public string SubmitterName { get; set; }
        public string SubmitterEmail { get; set; }
        public string SubmitterPhone { get; set; }
    }
}