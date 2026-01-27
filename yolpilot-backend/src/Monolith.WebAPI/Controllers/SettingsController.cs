using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolith.WebAPI.Applications.Commands.Settings;
using Monolith.WebAPI.Applications.Queries.Settings;
using Monolith.WebAPI.Extensions;
using Monolith.WebAPI.Responses.Settings;
using Swashbuckle.AspNetCore.Annotations;

namespace Monolith.WebAPI.Controllers;

[Route("api/settings")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
[SwaggerControllerOrder(14)]
public class SettingsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Get workspace settings (company, regional, etc.)
    /// </summary>
    [HttpGet("workspace")]
    [SwaggerOperation(Summary = "Get workspace settings")]
    public async Task<WorkspaceSettingsResponse> GetWorkspaceSettings()
    {
        var query = new GetWorkspaceSettingsQuery
        {
            AuthenticatedUserId = User.GetId()
        };
        return await sender.Send(query);
    }

    /// <summary>
    /// Update workspace settings (company, regional, etc.)
    /// Requires Admin role
    /// </summary>
    [HttpPut("workspace")]
    [SwaggerOperation(Summary = "Update workspace settings")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateWorkspaceSettings([FromBody] UpdateWorkspaceSettingsCommand command)
    {
        command.AuthenticatedUserId = User.GetId();
        await sender.Send(command);
        return Ok(new { message = "Workspace settings updated successfully" });
    }

    /// <summary>
    /// Get delivery settings
    /// </summary>
    [HttpGet("delivery")]
    [SwaggerOperation(Summary = "Get delivery settings")]
    public async Task<DeliverySettingsResponse> GetDeliverySettings()
    {
        var query = new GetDeliverySettingsQuery
        {
            AuthenticatedUserId = User.GetId()
        };
        return await sender.Send(query);
    }

    /// <summary>
    /// Update delivery settings
    /// Requires Dispatcher role
    /// </summary>
    [HttpPut("delivery")]
    [SwaggerOperation(Summary = "Update delivery settings")]
    [Authorize(Roles = "Dispatcher,Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateDeliverySettings([FromBody] UpdateDeliverySettingsCommand command)
    {
        command.AuthenticatedUserId = User.GetId();
        await sender.Send(command);
        return Ok(new { message = "Delivery settings updated successfully" });
    }

    /// <summary>
    /// Get notification settings
    /// </summary>
    [HttpGet("notifications")]
    [SwaggerOperation(Summary = "Get notification settings")]
    public async Task<NotificationSettingsResponse> GetNotificationSettings()
    {
        var query = new GetNotificationSettingsQuery
        {
            AuthenticatedUserId = User.GetId()
        };
        return await sender.Send(query);
    }

    /// <summary>
    /// Update notification settings
    /// Requires Dispatcher role
    /// </summary>
    [HttpPut("notifications")]
    [SwaggerOperation(Summary = "Update notification settings")]
    [Authorize(Roles = "Dispatcher,Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateNotificationSettings([FromBody] UpdateNotificationSettingsCommand command)
    {
        command.AuthenticatedUserId = User.GetId();
        await sender.Send(command);
        return Ok(new { message = "Notification settings updated successfully" });
    }

    // ✅ THEME ENDPOINTS KALDIRILDI

    /// <summary>
    /// Get delay alert settings
    /// </summary>
    [HttpGet("delay-alerts")]
    [SwaggerOperation(Summary = "Get delay alert settings")]
    [Authorize(Roles = "Dispatcher,Admin,SuperAdmin")]
    public async Task<DelayAlertSettingsResponse> GetDelayAlertSettings()
    {
        var query = new GetDelayAlertSettingsQuery
        {
            AuthenticatedUserId = User.GetId()
        };
        return await sender.Send(query);
    }

    /// <summary>
    /// Update delay alert settings
    /// Requires Dispatcher role
    /// </summary>
    [HttpPut("delay-alerts")]
    [SwaggerOperation(Summary = "Update delay alert settings")]
    [Authorize(Roles = "Dispatcher,Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateDelayAlertSettings([FromBody] UpdateDelayAlertSettingsCommand command)
    {
        command.AuthenticatedUserId = User.GetId();
        await sender.Send(command);
        return Ok(new { message = "Delay alert settings updated successfully" });
    }

    /// <summary>
    /// Get all settings combined
    /// </summary>
    [HttpGet("all")]
    [SwaggerOperation(Summary = "Get all settings")]
    public async Task<AllSettingsResponse> GetAllSettings()
    {
        var userId = User.GetId();

        var workspace = await sender.Send(new GetWorkspaceSettingsQuery { AuthenticatedUserId = userId });
        var delivery = await sender.Send(new GetDeliverySettingsQuery { AuthenticatedUserId = userId });
        var notifications = await sender.Send(new GetNotificationSettingsQuery { AuthenticatedUserId = userId });
        // ✅ Theme query KALDIRILDI

        return new AllSettingsResponse
        {
            Workspace = workspace,
            Delivery = delivery,
            Notifications = notifications
            // ✅ Theme property KALDIRILDI
        };
    }
}