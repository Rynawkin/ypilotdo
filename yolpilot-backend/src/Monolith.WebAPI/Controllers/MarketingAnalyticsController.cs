using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Marketing;
using Monolith.WebAPI.Services.Marketing;

namespace Monolith.WebAPI.Controllers;

[ApiController]
[Route("api/marketing-analytics")]
[EnableCors("MarketingCors")]
public class MarketingAnalyticsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<MarketingAnalyticsController> _logger;

    public MarketingAnalyticsController(AppDbContext context, ILogger<MarketingAnalyticsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("track")]
    [AllowAnonymous]
    public async Task<IActionResult> TrackEvent([FromBody] TrackMarketingEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VisitorId) || string.IsNullOrWhiteSpace(request.SessionId))
        {
            return BadRequest("visitorId ve sessionId zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            return BadRequest("eventType zorunludur.");
        }

        var geoContext = MarketingTrackingHelpers.GetGeoContext(
            Request,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        var item = new MarketingAnalyticsEvent
        {
            VisitorId = request.VisitorId.Trim(),
            SessionId = request.SessionId.Trim(),
            EventType = request.EventType.Trim(),
            EventName = request.EventName?.Trim(),
            PagePath = request.PagePath?.Trim(),
            PageTitle = request.PageTitle?.Trim(),
            Referrer = request.Referrer?.Trim(),
            UtmSource = request.UtmSource?.Trim(),
            UtmMedium = request.UtmMedium?.Trim(),
            UtmCampaign = request.UtmCampaign?.Trim(),
            UtmContent = request.UtmContent?.Trim(),
            UtmTerm = request.UtmTerm?.Trim(),
            DeviceType = request.DeviceType?.Trim(),
            Browser = request.Browser?.Trim(),
            Os = request.Os?.Trim(),
            MetadataJson = request.MetadataJson?.Trim(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            IpHash = MarketingTrackingHelpers.HashIp(geoContext.IpAddress),
            IpAddress = geoContext.IpAddress,
            CountryCode = geoContext.CountryCode,
            CountryName = geoContext.CountryName,
            Region = geoContext.Region,
            City = geoContext.City,
            OccurredAt = request.OccurredAt ?? DateTime.UtcNow
        };

        _context.MarketingAnalyticsEvents.Add(item);
        await _context.SaveChangesAsync();

        return Ok(new { tracked = true });
    }

    [HttpGet("overview")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetOverview([FromQuery] int days = 30)
    {
        days = Math.Clamp(days, 7, 90);
        var since = DateTime.UtcNow.AddDays(-days);

        var eventsQuery = _context.MarketingAnalyticsEvents
            .AsNoTracking()
            .Where(x => x.OccurredAt >= since);

        var leadsQuery = _context.MarketingLeads
            .AsNoTracking()
            .Where(x => x.CreatedAt >= since);

        var totalVisitors = await eventsQuery
            .Where(x => x.EventType == "page_view")
            .Select(x => x.VisitorId)
            .Distinct()
            .CountAsync();

        var totalSessions = await eventsQuery
            .Select(x => x.SessionId)
            .Distinct()
            .CountAsync();

        var pageViews = await eventsQuery.CountAsync(x => x.EventType == "page_view");
        var ctaClicks = await eventsQuery.CountAsync(x => x.EventType == "cta_click");
        var formSubmits = await eventsQuery.CountAsync(x => x.EventType == "form_submit");
        var leads = await leadsQuery.CountAsync();

        var topPages = await eventsQuery
            .Where(x => x.EventType == "page_view" && x.PagePath != null)
            .GroupBy(x => x.PagePath!)
            .Select(group => new
            {
                page = group.Key,
                views = group.Count(),
                visitors = group.Select(x => x.VisitorId).Distinct().Count()
            })
            .OrderByDescending(x => x.views)
            .Take(10)
            .ToListAsync();

        var topCampaigns = await eventsQuery
            .Where(x => x.EventType == "page_view")
            .GroupBy(x => new
            {
                source = x.UtmSource ?? "direct",
                medium = x.UtmMedium ?? "none",
                campaign = x.UtmCampaign ?? "organic"
            })
            .Select(group => new
            {
                source = group.Key.source,
                medium = group.Key.medium,
                campaign = group.Key.campaign,
                sessions = group.Select(x => x.SessionId).Distinct().Count(),
                visitors = group.Select(x => x.VisitorId).Distinct().Count()
            })
            .OrderByDescending(x => x.sessions)
            .Take(10)
            .ToListAsync();

        var topLocations = await eventsQuery
            .Where(x => x.EventType == "page_view" && (x.City != null || x.Region != null || x.CountryName != null))
            .GroupBy(x => new
            {
                city = x.City ?? "Bilinmiyor",
                region = x.Region ?? "Bilinmiyor",
                country = x.CountryName ?? x.CountryCode ?? "Bilinmiyor"
            })
            .Select(group => new
            {
                city = group.Key.city,
                region = group.Key.region,
                country = group.Key.country,
                sessions = group.Select(x => x.SessionId).Distinct().Count(),
                visitors = group.Select(x => x.VisitorId).Distinct().Count(),
                pageViews = group.Count()
            })
            .OrderByDescending(x => x.visitors)
            .ThenByDescending(x => x.pageViews)
            .Take(10)
            .ToListAsync();

        var trend = await eventsQuery
            .GroupBy(x => x.OccurredAt.Date)
            .Select(group => new
            {
                date = group.Key,
                pageViews = group.Count(x => x.EventType == "page_view"),
                ctaClicks = group.Count(x => x.EventType == "cta_click"),
                formSubmits = group.Count(x => x.EventType == "form_submit"),
                visitors = group.Select(x => x.VisitorId).Distinct().Count()
            })
            .OrderBy(x => x.date)
            .ToListAsync();

        var leadTrend = await leadsQuery
            .GroupBy(x => x.CreatedAt.Date)
            .Select(group => new
            {
                date = group.Key,
                leads = group.Count()
            })
            .OrderBy(x => x.date)
            .ToListAsync();

        var recentEvents = await eventsQuery
            .OrderByDescending(x => x.OccurredAt)
            .Take(600)
            .ToListAsync();

        var recentSessions = recentEvents
            .GroupBy(x => x.SessionId)
            .Select(group =>
            {
                var ordered = group.OrderBy(x => x.OccurredAt).ToList();
                var landing = ordered.FirstOrDefault(x => x.EventType == "page_view")?.PagePath ?? ordered.FirstOrDefault()?.PagePath;
                return new
                {
                    sessionId = group.Key,
                    visitorId = group.First().VisitorId,
                    firstSeen = ordered.First().OccurredAt,
                    lastSeen = ordered.Last().OccurredAt,
                    landingPage = landing,
                    source = ordered.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.UtmSource))?.UtmSource ?? "direct",
                    medium = ordered.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.UtmMedium))?.UtmMedium ?? "none",
                    campaign = ordered.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.UtmCampaign))?.UtmCampaign ?? "organic",
                    ipAddress = ordered.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.IpAddress))?.IpAddress,
                    city = ordered.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.City))?.City,
                    region = ordered.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Region))?.Region,
                    country = ordered.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.CountryName))?.CountryName
                              ?? ordered.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.CountryCode))?.CountryCode,
                    referrer = ordered.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Referrer))?.Referrer,
                    browser = ordered.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Browser))?.Browser,
                    os = ordered.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Os))?.Os,
                    deviceType = ordered.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.DeviceType))?.DeviceType,
                    pageViews = ordered.Count(x => x.EventType == "page_view"),
                    ctaClicks = ordered.Count(x => x.EventType == "cta_click"),
                    formSubmits = ordered.Count(x => x.EventType == "form_submit")
                };
            })
            .OrderByDescending(x => x.lastSeen)
            .Take(30)
            .ToList();

        var visitorToLeadRate = totalVisitors > 0
            ? Math.Round((double)leads / totalVisitors * 100, 2)
            : 0;

        return Ok(new
        {
            rangeDays = days,
            totals = new
            {
                totalVisitors,
                totalSessions,
                pageViews,
                ctaClicks,
                formSubmits,
                leads,
                visitorToLeadRate
            },
            topPages,
            topCampaigns,
            topLocations,
            trend,
            leadTrend,
            recentSessions
        });
    }

    [HttpGet("recent-leads")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetRecentLeads([FromQuery] int take = 20)
    {
        take = Math.Clamp(take, 5, 100);

        var leads = await _context.MarketingLeads
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Email,
                x.Company,
                x.Source,
                x.SelectedPlan,
                x.Status,
                x.LandingPage,
                x.UtmSource,
                x.UtmMedium,
                x.UtmCampaign,
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(leads);
    }
}

public class TrackMarketingEventRequest
{
    public string VisitorId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? EventName { get; set; }
    public string? PagePath { get; set; }
    public string? PageTitle { get; set; }
    public string? Referrer { get; set; }
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
    public string? UtmContent { get; set; }
    public string? UtmTerm { get; set; }
    public string? DeviceType { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime? OccurredAt { get; set; }
}
