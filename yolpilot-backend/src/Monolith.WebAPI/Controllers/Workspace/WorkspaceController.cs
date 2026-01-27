using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolith.WebAPI.Applications.Commands.Workspace;
using Monolith.WebAPI.Applications.Queries.Workspace;
using Monolith.WebAPI.Extensions;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;
using Swashbuckle.AspNetCore.Annotations;

namespace Monolith.WebAPI.Controllers.Workspace;

[ApiController]
[Route("workspace")]
[SwaggerControllerOrder(2)]
public class WorkspaceController(ISender sender, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation("Create a new workspace")]
    public async Task<TokenModel> CreateWorkspace(CreateWorkspaceCommand command)
    {
        var isAuthenticated = httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated;
        if (isAuthenticated.HasValue && isAuthenticated.Value)
            throw new ApiException("You cannot create a workspace while authenticated.", 400);

        return await sender.Send(command);
    }

    [Authorize(AuthenticationSchemes = "Bearer")]
    [HttpGet]
    public async Task<WorkspaceResponse> GetWorkspace()
    {
        // ✅ DÜZELTME: Property initialization kullan
        var query = new GetWorkspaceQuery
        {
            AuthenticatedUserId = User.GetId()
        };
        return await sender.Send(query);
    }

    [Authorize(AuthenticationSchemes = "Bearer")]
    [HttpPut]
    public async Task<WorkspaceResponse> UpdateWorkspace(UpdateWorkspaceCommand command)
    {
        command.AuthenticatedUserId = User.GetId();
        var workspace = await sender.Send(command);
        return workspace;
    }
}