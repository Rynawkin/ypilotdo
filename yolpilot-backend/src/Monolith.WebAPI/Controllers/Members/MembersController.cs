using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolith.WebAPI.Applications.Commands.Members;
using Monolith.WebAPI.Applications.Queries.Members;
using Monolith.WebAPI.Extensions;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Members;
using Monolith.WebAPI.Services.Members;
using Swashbuckle.AspNetCore.Annotations;

namespace Monolith.WebAPI.Controllers.Members;

[ApiController]
[Route("api/members")]  // api/ prefix'ini ekle
[SwaggerControllerOrder(1)]
public class MembersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Get all members of the workspace.")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IEnumerable<UserResponse>> GetAllMembers(
        [FromQuery] bool? isDriver = null,
        [FromQuery] bool? isDispatcher = null,
        [FromQuery] bool? isAdmin = null,
        [FromQuery] string? searchQuery = null)
    {
        // ✅ DÜZELTME: Property initialization ve tüm filtreleri ekle
        var query = new GetMembersQuery
        {
            AuthenticatedUserId = User.GetId(),
            IsDriver = isDriver,
            IsDispatcher = isDispatcher,
            IsAdmin = isAdmin,
            SearchQuery = searchQuery
        };
        return await sender.Send(query);
    }

    [HttpPost("create-dispatcher")]
    [SwaggerOperation(Summary = "Create a new dispatcher user directly")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<UserResponse> CreateDispatcher(CreateDispatcherCommand command)
    {
        command.AuthenticatedUserId = User.GetId();
        return await sender.Send(command);
    }

    [HttpPut("{userId:guid}")]
    [SwaggerOperation(Summary = "Update the member roles, depot. All fields are optional.")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<UserResponse> UpdateMember(Guid userId, UpdateMemberCommand command)
    {
        command.UserId = userId;
        command.AuthenticatedUserId = User.GetId();
        return await sender.Send(command);
    }

    [HttpPut("{userId:guid}/roles")]
    [SwaggerOperation(Summary =
        "Update the member roles (admin, driver, dispatcher), all roles are optional, and nullable so you can update only the roles you want.")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<UserResponse> UpdateMemberRoles(Guid userId, UpdateMemberRoleCommand command)
    {
        command.UserId = userId;
        command.AuthenticatedUserId = User.GetId();
        return await sender.Send(command);
    }

    [HttpDelete("{userId:guid}")]
    [SwaggerOperation(Summary = "Delete a member from workspace (soft delete)")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> DeleteMember(Guid userId)
    {
        var command = new DeleteMemberCommand
        {
            UserId = userId,
            AuthenticatedUserId = User.GetId()
        };
        
        await sender.Send(command);
        return NoContent();
    }
    
    [HttpPut("{userId:guid}/depot")]
    [SwaggerOperation(Summary = "Update the member depot")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<UserResponse> UpdateMemberDepot(Guid userId, UpdateMemberDepotCommand command)
    {
        command.UserId = userId;
        command.AuthenticatedUserId = User.GetId();
        return await sender.Send(command);
    }
}