using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Monolith.WebAPI.Services.Subscription;
using Monolith.WebAPI.Services.WhatsApp;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Extensions;

namespace Monolith.WebAPI.Controllers;

[ApiController]
[Route("api/subscription")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IWhatsAppRateLimiter _whatsAppRateLimiter;

    public SubscriptionController(
        ISubscriptionService subscriptionService,
        IWhatsAppRateLimiter whatsAppRateLimiter)
    {
        _subscriptionService = subscriptionService;
        _whatsAppRateLimiter = whatsAppRateLimiter;
    }

    [HttpGet("usage")]
    public async Task<IActionResult> GetCurrentUsage()
    {
        var workspaceId = User.GetWorkspaceId();
        if (workspaceId == 0)
            return Unauthorized("Workspace not found");

        var usage = await _subscriptionService.GetCurrentUsage(workspaceId);
        if (usage == null)
            return NotFound("Usage data not found");
            
        return Ok(usage);
    }

    [HttpGet("plan")]
    public async Task<IActionResult> GetPlanDetails()
    {
        var workspaceId = User.GetWorkspaceId();
        if (workspaceId == 0)
            return Unauthorized("Workspace not found");

        var usage = await _subscriptionService.GetCurrentUsage(workspaceId);
        if (usage == null)
            return NotFound("Usage data not found");
            
        var limits = _subscriptionService.GetPlanLimits(usage.PlanType);
        
        return Ok(new
        {
            CurrentPlan = usage.PlanType.ToString(),
            Limits = limits,
            CurrentUsage = usage
        });
    }

    [HttpGet("whatsapp-usage")]
    public async Task<IActionResult> GetWhatsAppUsage()
    {
        var workspaceId = User.GetWorkspaceId();
        if (workspaceId == 0)
            return Unauthorized("Workspace not found");

        var stats = await _whatsAppRateLimiter.GetUsageStats(workspaceId);
        
        return Ok(stats);
    }

    [HttpGet("whatsapp-limits")]
    public async Task<IActionResult> GetWhatsAppLimits()
    {
        var workspaceId = User.GetWorkspaceId();
        if (workspaceId == 0)
            return Unauthorized("Workspace not found");

        var rateLimitResult = await _whatsAppRateLimiter.CheckRateLimit(workspaceId);
        var stats = await _whatsAppRateLimiter.GetUsageStats(workspaceId);
        
        return Ok(new
        {
            CanSend = rateLimitResult.CanSend,
            Reason = rateLimitResult.Reason,
            NextAvailableTime = rateLimitResult.NextAvailableTime,
            RemainingMessages = rateLimitResult.RemainingMessages,
            CurrentUsage = new
            {
                LastMinute = stats.MessagesLastMinute,
                LastHour = stats.MessagesLastHour,
                Today = stats.MessagesToday,
                ThisMonth = stats.MessagesThisMonth
            },
            Limits = new
            {
                PerMinute = stats.MaxPerMinute,
                PerHour = stats.MaxPerHour,
                PerDay = stats.MaxPerDay,
                PerMonth = stats.MaxPerMonth
            },
            Remaining = new
            {
                Today = stats.RemainingToday,
                ThisMonth = stats.RemainingThisMonth,
                TwilioRequests = stats.TwilioRemainingRequests
            }
        });
    }

    [HttpPost("reset-monthly-usage")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ResetMonthlyUsage()
    {
        var workspaceId = User.GetWorkspaceId();
        if (workspaceId == 0)
            return BadRequest("Workspace not found");

        await _subscriptionService.ResetMonthlyUsage(workspaceId);
        
        return Ok(new { Message = "Monthly usage has been reset" });
    }

    [HttpGet("plan-limits/{planType}")]
    public IActionResult GetPlanLimits(string planType)
    {
        // PlanType enum'ını parse et
        if (!Enum.TryParse<PlanType>(planType, true, out var parsedPlanType))
        {
            return BadRequest($"Invalid plan type: {planType}");
        }

        var limits = _subscriptionService.GetPlanLimits(parsedPlanType);
        return Ok(limits);
    }

    [HttpGet("billing-summary")]
    public async Task<IActionResult> GetBillingSummary()
    {
        var workspaceId = User.GetWorkspaceId();
        if (workspaceId == 0)
            return Unauthorized("Workspace not found");

        var usage = await _subscriptionService.GetCurrentUsage(workspaceId);
        if (usage == null)
            return NotFound("Usage data not found");
            
        var limits = _subscriptionService.GetPlanLimits(usage.PlanType);
        var whatsAppStats = await _whatsAppRateLimiter.GetUsageStats(workspaceId);
        
        // Ek ücret hesaplamaları
        var extraStops = Math.Max(0, usage.CurrentMonthStops - limits.IncludedMonthlyStops);
        var extraStopCharges = extraStops * limits.AdditionalStopPrice;
        
        var extraWhatsApp = Math.Max(0, usage.CurrentMonthWhatsAppMessages - limits.IncludedWhatsAppMessages);
        var extraWhatsAppCharges = extraWhatsApp * limits.AdditionalWhatsAppPrice;
        
        return Ok(new
        {
            Plan = new
            {
                Name = usage.PlanType.ToString(),
                MonthlyPrice = limits.MonthlyPrice,
                Currency = "TRY"
            },
            Usage = new
            {
                Stops = new
                {
                    Used = usage.CurrentMonthStops,
                    Included = limits.IncludedMonthlyStops,
                    Extra = extraStops,
                    ExtraUnitPrice = limits.AdditionalStopPrice,
                    ExtraCharges = extraStopCharges
                },
                WhatsApp = new
                {
                    Used = usage.CurrentMonthWhatsAppMessages,
                    Included = limits.IncludedWhatsAppMessages,
                    Extra = extraWhatsApp,
                    ExtraUnitPrice = limits.AdditionalWhatsAppPrice,
                    ExtraCharges = extraWhatsAppCharges
                }
            },
            Summary = new
            {
                BasePrice = limits.MonthlyPrice,
                AdditionalCharges = usage.CurrentMonthAdditionalCharges,
                EstimatedTotal = usage.EstimatedMonthlyTotal,
                Currency = "TRY",
                BillingPeriod = new
                {
                    Start = usage.LastResetDate,
                    End = usage.NextResetDate
                }
            }
        });
    }
}