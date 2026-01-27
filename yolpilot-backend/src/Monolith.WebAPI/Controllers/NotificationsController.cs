using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolith.WebAPI.Applications.Commands.Notifications;
using Monolith.WebAPI.Applications.Queries.Notifications;
using Monolith.WebAPI.Extensions;
using Monolith.WebAPI.Responses.Notifications;
using Swashbuckle.AspNetCore.Annotations;

namespace Monolith.WebAPI.Controllers;

[Route("api/notifications")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
[SwaggerControllerOrder(16)]
public class NotificationsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Get user notifications with pagination and filtering
    /// </summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Get user notifications")]
    public async Task<NotificationsListResponse> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isRead = null,
        [FromQuery] string? type = null)
    {
        var query = new GetNotificationsQuery
        {
            AuthenticatedUserId = User.GetId(),
            Page = page,
            PageSize = pageSize,
            IsRead = isRead,
            Type = type
        };
        return await sender.Send(query);
    }

    /// <summary>
    /// Get unread notification count
    /// </summary>
    [HttpGet("unread-count")]
    [SwaggerOperation(Summary = "Get unread notifications count")]
    public async Task<UnreadCountResponse> GetUnreadCount()
    {
        var query = new GetUnreadNotificationCountQuery
        {
            AuthenticatedUserId = User.GetId()
        };
        return await sender.Send(query);
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    [HttpPut("{id}/mark-read")]
    [SwaggerOperation(Summary = "Mark notification as read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var command = new MarkNotificationAsReadCommand
        {
            AuthenticatedUserId = User.GetId(),
            NotificationId = id
        };
        
        var result = await sender.Send(command);
        return result ? Ok() : NotFound();
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPut("mark-all-read")]
    [SwaggerOperation(Summary = "Mark all notifications as read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var command = new MarkAllNotificationsAsReadCommand
        {
            AuthenticatedUserId = User.GetId()
        };
        
        await sender.Send(command);
        return Ok();
    }

    /// <summary>
    /// Delete a notification
    /// </summary>
    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "Delete notification")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        var command = new DeleteNotificationCommand
        {
            AuthenticatedUserId = User.GetId(),
            NotificationId = id
        };
        
        var result = await sender.Send(command);
        return result ? Ok() : NotFound();
    }

    /// <summary>
    /// Clear all read notifications
    /// </summary>
    [HttpDelete("clear-read")]
    [SwaggerOperation(Summary = "Clear all read notifications")]
    public async Task<IActionResult> ClearReadNotifications()
    {
        var command = new ClearReadNotificationsCommand
        {
            AuthenticatedUserId = User.GetId()
        };
        
        await sender.Send(command);
        return Ok();
    }

    /// <summary>
    /// Create a system announcement (Admin/SuperAdmin only)
    /// </summary>
    [HttpPost("announcement")]
    [SwaggerOperation(Summary = "Create system announcement")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CreateAnnouncement([FromBody] CreateAnnouncementCommand command)
    {
        command.AuthenticatedUserId = User.GetId();
        await sender.Send(command);
        return Ok();
    }
}