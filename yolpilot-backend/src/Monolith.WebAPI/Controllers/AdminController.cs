using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolith.WebAPI.Applications.Queries.Admin;
using Monolith.WebAPI.Applications.Commands.Admin;
using Monolith.WebAPI.Extensions;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Admin;
using Monolith.WebAPI.Services.Subscription;
using Monolith.WebAPI.Data.Workspace;
using Swashbuckle.AspNetCore.Annotations;

namespace Monolith.WebAPI.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(AuthenticationSchemes = "Bearer", Roles = "SuperAdmin")]
[SwaggerControllerOrder(10)]
public class AdminController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ISubscriptionService _subscriptionService;

    public AdminController(ISender sender, ISubscriptionService subscriptionService)
    {
        _sender = sender;
        _subscriptionService = subscriptionService;
    }

    /// <summary>
    /// Get workspace statistics for super admin dashboard
    /// </summary>
    [HttpGet("workspaces/stats")]
    [SwaggerOperation("Get workspace statistics")]
    public async Task<WorkspaceStatsResponse> GetWorkspaceStats()
    {
        var query = new GetWorkspaceStatsQuery();
        return await _sender.Send(query);
    }

    /// <summary>
    /// Get workspace usage details for super admin dashboard
    /// </summary>
    [HttpGet("workspaces/usage")]
    [SwaggerOperation("Get workspace usage statistics")]
    public async Task<List<WorkspaceUsageResponse>> GetWorkspaceUsage()
    {
        var query = new GetWorkspaceUsageQuery();
        return await _sender.Send(query);
    }

    /// <summary>
    /// Get all workspaces for super admin
    /// </summary>
    [HttpGet("workspaces")]
    [SwaggerOperation("Get all workspaces")]
    public async Task<List<WorkspaceListResponse>> GetAllWorkspaces()
    {
        var query = new GetAllWorkspacesQuery();
        return await _sender.Send(query);
    }

    /// <summary>
    /// Get specific workspace details
    /// </summary>
    [HttpGet("workspaces/{id}")]
    [SwaggerOperation("Get workspace by ID")]
    public async Task<WorkspaceDetailResponse> GetWorkspaceById(int id)
    {
        var query = new GetWorkspaceByIdQuery(id);
        return await _sender.Send(query);
    }

    /// <summary>
    /// Update workspace status (active/inactive)
    /// </summary>
    [HttpPatch("workspaces/{id}/status")]
    [SwaggerOperation("Update workspace status")]
    public async Task<WorkspaceDetailResponse> UpdateWorkspaceStatus(int id, [FromBody] UpdateWorkspaceStatusRequest request)
    {
        var command = new UpdateWorkspaceStatusCommand
        {
            WorkspaceId = id,
            Active = request.Active,
            AuthenticatedUserId = User.GetId()
        };
        return await _sender.Send(command);
    }

    /// <summary>
    /// Delete/Archive workspace (soft delete)
    /// </summary>
    [HttpDelete("workspaces/{id}")]
    [SwaggerOperation("Delete workspace")]
    public async Task<IActionResult> DeleteWorkspace(int id)
    {
        var command = new DeleteWorkspaceCommand
        {
            WorkspaceId = id,
            AuthenticatedUserId = User.GetId()
        };
        
        var result = await _sender.Send(command);
        if (result)
            return Ok(new { message = "Workspace deleted successfully" });
        
        return NotFound(new { message = "Workspace not found" });
    }

    /// <summary>
    /// Get workspace subscription details
    /// </summary>
    [HttpGet("workspaces/{id}/subscription")]
    [SwaggerOperation("Get workspace subscription details")]
    public async Task<IActionResult> GetWorkspaceSubscription(int id)
    {
        var usage = await _subscriptionService.GetCurrentUsage(id);
        if (usage == null)
            return NotFound(new { message = "Workspace not found" });
            
        var workspace = await _sender.Send(new GetWorkspaceByIdQuery(id));
        var limits = _subscriptionService.GetPlanLimits(workspace.PlanType);
        
        return Ok(new
        {
            CurrentPlan = workspace.PlanType.ToString(),
            PlanStartDate = workspace.PlanStartDate,
            PlanEndDate = workspace.PlanEndDate,
            Limits = limits,
            CurrentUsage = usage
        });
    }

    /// <summary>
    /// Update workspace plan
    /// </summary>
    [HttpPut("workspaces/{id}/plan")]
    [SwaggerOperation("Update workspace plan")]
    public async Task<IActionResult> UpdateWorkspacePlan(int id, [FromBody] UpdateWorkspacePlanRequest request)
    {
        var command = new UpdateWorkspacePlanCommand
        {
            WorkspaceId = id,
            NewPlanType = request.PlanType,
            AuthenticatedUserId = User.GetId()
        };
        
        var result = await _sender.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Reset workspace monthly usage
    /// </summary>
    [HttpPost("workspaces/{id}/reset-usage")]
    [SwaggerOperation("Reset workspace monthly usage")]
    public async Task<IActionResult> ResetWorkspaceUsage(int id)
    {
        await _subscriptionService.ResetMonthlyUsage(id);
        var usage = await _subscriptionService.GetCurrentUsage(id);
        
        if (usage == null)
            return NotFound(new { message = "Workspace not found" });
            
        return Ok(new
        {
            Message = "Usage reset successfully",
            CurrentUsage = usage
        });
    }

    /// <summary>
    /// Get all available plans
    /// </summary>
    [HttpGet("plans")]
    [SwaggerOperation("Get all available plans")]
    public IActionResult GetAvailablePlans()
    {
        var plans = new[]
        {
            new
            {
                planType = "Starter",
                planTypeValue = 1,
                limits = _subscriptionService.GetPlanLimits(PlanType.Starter)
            },
            new
            {
                planType = "Growth", 
                planTypeValue = 2,
                limits = _subscriptionService.GetPlanLimits(PlanType.Growth)
            },
            new
            {
                planType = "Professional",
                planTypeValue = 3,
                limits = _subscriptionService.GetPlanLimits(PlanType.Professional)
            },
            new
            {
                planType = "Business",
                planTypeValue = 4,
                limits = _subscriptionService.GetPlanLimits(PlanType.Business)
            }
        };
        
        return Ok(plans);
    }

    /// <summary>
    /// Get workspace usage summary for billing
    /// </summary>
    [HttpGet("workspaces/{id}/billing")]
    [SwaggerOperation("Get workspace billing summary")]
    public async Task<IActionResult> GetWorkspaceBilling(int id)
    {
        var usage = await _subscriptionService.GetCurrentUsage(id);
        if (usage == null)
            return NotFound(new { message = "Workspace not found" });
        
        var workspace = await _sender.Send(new GetWorkspaceByIdQuery(id));
        var limits = _subscriptionService.GetPlanLimits(workspace.PlanType);
        
        // Ek ücret hesaplamaları
        var extraStopCharges = 0m;
        var extraWhatsAppCharges = 0m;
        
        if (usage.CurrentMonthStops > limits.IncludedMonthlyStops)
        {
            var extraStops = usage.CurrentMonthStops - limits.IncludedMonthlyStops;
            extraStopCharges = extraStops * limits.AdditionalStopPrice;
        }
        
        if (usage.CurrentMonthWhatsAppMessages > limits.IncludedWhatsAppMessages)
        {
            var extraMessages = usage.CurrentMonthWhatsAppMessages - limits.IncludedWhatsAppMessages;
            extraWhatsAppCharges = extraMessages * limits.AdditionalWhatsAppPrice;
        }
        
        return Ok(new
        {
            WorkspaceId = id,
            WorkspaceName = workspace.Name,
            CurrentPlan = workspace.PlanType.ToString(),
            MonthlyBaseFee = limits.MonthlyPrice,
            
            // Durak kullanımı
            IncludedStops = limits.IncludedMonthlyStops,
            UsedStops = usage.CurrentMonthStops,
            ExtraStops = Math.Max(0, usage.CurrentMonthStops - limits.IncludedMonthlyStops),
            ExtraStopUnitPrice = limits.AdditionalStopPrice,
            ExtraStopCharges = extraStopCharges,
            
            // WhatsApp kullanımı
            IncludedWhatsApp = limits.IncludedWhatsAppMessages,
            UsedWhatsApp = usage.CurrentMonthWhatsAppMessages,
            ExtraWhatsApp = Math.Max(0, usage.CurrentMonthWhatsAppMessages - limits.IncludedWhatsAppMessages),
            ExtraWhatsAppUnitPrice = limits.AdditionalWhatsAppPrice,
            ExtraWhatsAppCharges = extraWhatsAppCharges,
            
            // Toplam
            TotalAdditionalCharges = usage.CurrentMonthAdditionalCharges,
            EstimatedMonthlyTotal = usage.EstimatedMonthlyTotal,
            
            // Tarihler
            BillingPeriodStart = usage.LastResetDate,
            BillingPeriodEnd = usage.NextResetDate
        });
    }
}

// Request Models
public class UpdateWorkspaceStatusRequest
{
    public bool Active { get; set; }
}

public class UpdateWorkspacePlanRequest
{
    public PlanType PlanType { get; set; }
}