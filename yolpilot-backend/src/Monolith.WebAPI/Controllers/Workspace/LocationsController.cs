using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolith.WebAPI.Applications.Commands.Workspace;
using Monolith.WebAPI.Applications.Queries.Workspace;
using Monolith.WebAPI.Extensions;
using Monolith.WebAPI.Responses.Workspace;
using Swashbuckle.AspNetCore.Annotations;

namespace Monolith.WebAPI.Controllers.Workspace;

[ApiController]
[Route("locations")]
[SwaggerControllerOrder(4)]
[Authorize(AuthenticationSchemes = "Bearer")]
public class LocationsController(ISender sender) : ControllerBase
{
    [SwaggerOperation(Summary = "Create a new saved location for the workspace.")]
    [HttpPost]
    public async Task<SavedLocationResponse> CreateSavedLocation(CreateSavedLocationCommand command)
    {
        command.AuthenticatedUserId = User.GetId();
        return await sender.Send(command);
    }

    [SwaggerOperation(Summary = "Get all saved locations of the workspace.")]
    [HttpGet]
    public async Task<IEnumerable<SavedLocationResponse>> GetSavedLocations()
    {
        var query = new GetSavedLocationsQuery(User.GetWorkspaceId());
        return await sender.Send(query);
    }

    [HttpPut("{locationId:int}")]
    [SwaggerOperation(Summary = "Update a saved location by id.")]
    public async Task<SavedLocationResponse> UpdateSavedLocation(int locationId, UpdateSavedLocationCommand command)
    {
        command.AuthenticatedUserId = User.GetId();
        command.SavedLocationId = locationId;

        return await sender.Send(command);
    }
}