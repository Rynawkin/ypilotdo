using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Services.Feedback
{
    public class FeedbackService : IFeedbackService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public FeedbackService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public string GenerateFeedbackToken(int journeyId, int stopId, int customerId)
        {
            // Journey'den workspaceId'yi al
            var journey = _context.Journeys.FirstOrDefault(j => j.Id == journeyId);
            if (journey == null)
                throw new ArgumentException($"Journey {journeyId} not found");

            var workspaceId = journey.WorkspaceId;
            var secret = _configuration["Tracking:Secret"] ?? "YolPilot2024Secret!";
            
            // Token'a workspaceId'yi de ekle
            var tokenData = $"{workspaceId}|{journeyId}|{stopId}|{customerId}|{DateTime.UtcNow.Ticks}";
            var input = $"feedback-{tokenData}-{secret}";
            
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            var token = Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Replace("=", "");
            
            // Token'ın sonuna encode edilmiş data'yı ekle
            var encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenData))
                .Replace("+", "-").Replace("/", "_").Replace("=", "");
            
            return $"{token}.{encodedData}";
        }

        public FeedbackTokenData ValidateAndExtractToken(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return null;

                // Token iki parçadan oluşuyor: hash.encodedData
                var parts = token.Split('.');
                if (parts.Length != 2)
                    return null;

                var encodedData = parts[1];
                
                // Base64 padding'i düzelt
                encodedData = encodedData.Replace("-", "+").Replace("_", "/");
                switch (encodedData.Length % 4)
                {
                    case 2: encodedData += "=="; break;
                    case 3: encodedData += "="; break;
                }

                var dataBytes = Convert.FromBase64String(encodedData);
                var tokenData = Encoding.UTF8.GetString(dataBytes);
                
                var dataParts = tokenData.Split('|');
                if (dataParts.Length < 5)
                    return null;

                // Token'ın geçerlilik süresini kontrol et (24 saat)
                if (long.TryParse(dataParts[4], out var ticks))
                {
                    var tokenTime = new DateTime(ticks);
                    if (DateTime.UtcNow.Subtract(tokenTime).TotalHours > 24)
                        return null; // Token süresi dolmuş
                }

                return new FeedbackTokenData
                {
                    WorkspaceId = int.Parse(dataParts[0]),
                    JourneyId = int.Parse(dataParts[1]),
                    StopId = int.Parse(dataParts[2]),
                    CustomerId = int.Parse(dataParts[3])
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<CustomerFeedback> SubmitFeedbackAsync(
            string token, 
            int overallRating, 
            int? deliverySpeed, 
            int? driverBehavior, 
            int? packageCondition, 
            string comments,
            string ipAddress,
            string userAgent)
        {
            // Token'dan bilgileri çıkar
            var tokenData = ValidateAndExtractToken(token);
            if (tokenData == null)
                throw new Exception("Invalid or expired feedback token");

            // Token ile mevcut feedback var mı kontrol et
            var existingFeedback = await _context.CustomerFeedback
                .FirstOrDefaultAsync(f => f.FeedbackToken == token);

            if (existingFeedback != null && existingFeedback.Comments != "[STUB]")
            {
                throw new Exception("Feedback already submitted");
            }

            CustomerFeedback feedback;
            
            if (existingFeedback != null)
            {
                // Mevcut kaydı güncelle (eğer daha önce stub olarak oluşturulduysa)
                feedback = existingFeedback;
                feedback.OverallRating = overallRating;
                feedback.DeliverySpeedRating = deliverySpeed;
                feedback.DriverBehaviorRating = driverBehavior;
                feedback.PackageConditionRating = packageCondition;
                feedback.Comments = comments;
                feedback.IpAddress = ipAddress;
                feedback.UserAgent = userAgent;
                feedback.SubmittedAt = DateTime.UtcNow;
            }
            else
            {
                // Yeni feedback kaydı oluştur
                feedback = new CustomerFeedback
                {
                    WorkspaceId = tokenData.WorkspaceId,
                    JourneyId = tokenData.JourneyId,
                    JourneyStopId = tokenData.StopId,
                    CustomerId = tokenData.CustomerId,
                    FeedbackToken = token,
                    OverallRating = overallRating,
                    DeliverySpeedRating = deliverySpeed,
                    DriverBehaviorRating = driverBehavior,
                    PackageConditionRating = packageCondition,
                    Comments = comments,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    SubmittedAt = DateTime.UtcNow
                };
                
                _context.CustomerFeedback.Add(feedback);
            }

            await _context.SaveChangesAsync();
            return feedback;
        }

        public async Task<CustomerFeedback> GetFeedbackByTokenAsync(string token)
        {
            return await _context.CustomerFeedback
                .Include(f => f.Customer)
                .Include(f => f.Journey)
                .Include(f => f.JourneyStop)
                .FirstOrDefaultAsync(f => f.FeedbackToken == token);
        }

        public async Task<List<CustomerFeedback>> GetWorkspaceFeedbackAsync(
            int workspaceId, 
            DateTime? startDate, 
            DateTime? endDate)
        {
            var query = _context.CustomerFeedback
                .Include(f => f.Customer)
                .Include(f => f.Journey)
                    .ThenInclude(j => j.Driver)
                .Where(f => f.WorkspaceId == workspaceId && f.Comments != "[STUB]"); // Sadece doldurulmuş feedbackler

            if (startDate.HasValue)
                query = query.Where(f => f.SubmittedAt >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(f => f.SubmittedAt < endDate.Value.Date.AddDays(1)); // Günün sonuna kadar dahil et

            return await query.OrderByDescending(f => f.SubmittedAt).ToListAsync();
        }

        public async Task<object> GetFeedbackStatsAsync(
            int workspaceId, 
            DateTime? startDate, 
            DateTime? endDate)
        {
            var query = _context.CustomerFeedback
                .Include(f => f.Journey)
                    .ThenInclude(j => j.Driver)
                .Include(f => f.Customer)
                .Where(f => f.WorkspaceId == workspaceId && f.Comments != "[STUB]");

            if (startDate.HasValue)
                query = query.Where(f => f.SubmittedAt >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(f => f.SubmittedAt < endDate.Value.Date.AddDays(1)); // Günün sonuna kadar dahil et

            var feedbacks = await query.ToListAsync();

            if (!feedbacks.Any())
            {
                return new
                {
                    totalFeedbacks = 0,
                    averageOverallRating = 0.0,
                    averageDeliverySpeedRating = 0.0,
                    averageDriverBehaviorRating = 0.0,
                    averagePackageConditionRating = 0.0,
                    ratingDistribution = new List<object>(),
                    topDrivers = new List<object>(),
                    bottomDrivers = new List<object>(),
                    trendsOverTime = new List<object>(),
                    recentFeedbacks = new List<object>()
                };
            }

            // Driver istatistikleri
            var driverStats = feedbacks
                .Where(f => f.Journey?.Driver != null)
                .GroupBy(f => f.Journey.Driver)
                .Select(g => new
                {
                    driverId = g.Key.Id,
                    driverName = g.Key.Name,
                    averageRating = g.Average(f => f.OverallRating),
                    feedbackCount = g.Count()
                })
                .OrderByDescending(d => d.averageRating)
                .ToList();

            // Rating dağılımı
            var ratingDist = Enumerable.Range(1, 5)
                .Select(rating => new
                {
                    rating = rating,
                    count = feedbacks.Count(f => f.OverallRating == rating),
                    percentage = Math.Round((double)feedbacks.Count(f => f.OverallRating == rating) / feedbacks.Count * 100, 1)
                })
                .ToList();

            // Zaman bazlı trend (günlük ortalamalar)
            var trends = feedbacks
                .GroupBy(f => f.SubmittedAt.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    averageRating = Math.Round(g.Average(f => f.OverallRating), 1)
                })
                .OrderBy(t => t.date)
                .ToList();

            // Son feedbackler
            var recentFeedbacksList = feedbacks
                .OrderByDescending(f => f.SubmittedAt)
                .Take(10)
                .Select(f => new
                {
                    id = f.Id,
                    overallRating = f.OverallRating,
                    deliverySpeedRating = f.DeliverySpeedRating,
                    driverBehaviorRating = f.DriverBehaviorRating,
                    packageConditionRating = f.PackageConditionRating,
                    comments = f.Comments,
                    submittedAt = f.SubmittedAt,
                    submitterName = f.SubmitterName,
                    submitterEmail = f.SubmitterEmail,
                    submitterPhone = f.SubmitterPhone,
                    customer = new
                    {
                        name = f.Customer?.Name ?? "Anonymous",
                        address = f.Customer?.Address ?? ""
                    },
                    driver = f.Journey?.Driver != null ? new
                    {
                        id = f.Journey.Driver.Id,
                        name = f.Journey.Driver.Name
                    } : null,
                    journey = f.Journey != null ? new
                    {
                        id = f.Journey.Id,
                        date = f.Journey.Date
                    } : null
                })
                .ToList();

            return new
            {
                totalFeedbacks = feedbacks.Count,
                averageOverallRating = Math.Round(feedbacks.Average(f => f.OverallRating), 1),
                averageDeliverySpeedRating = Math.Round(feedbacks.Where(f => f.DeliverySpeedRating.HasValue)
                    .Select(f => f.DeliverySpeedRating.Value)
                    .DefaultIfEmpty(0)
                    .Average(), 1),
                averageDriverBehaviorRating = Math.Round(feedbacks.Where(f => f.DriverBehaviorRating.HasValue)
                    .Select(f => f.DriverBehaviorRating.Value)
                    .DefaultIfEmpty(0)
                    .Average(), 1),
                averagePackageConditionRating = Math.Round(feedbacks.Where(f => f.PackageConditionRating.HasValue)
                    .Select(f => f.PackageConditionRating.Value)
                    .DefaultIfEmpty(0)
                    .Average(), 1),
                ratingDistribution = ratingDist,
                topDrivers = driverStats.Take(5),
                bottomDrivers = driverStats.Where(d => d.feedbackCount >= 3).TakeLast(5).Reverse(),
                trendsOverTime = trends,
                recentFeedbacks = recentFeedbacksList
            };
        }
    }
}