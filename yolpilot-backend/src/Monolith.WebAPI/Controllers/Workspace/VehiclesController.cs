using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolith.WebAPI.Applications.Commands.Vehicles;
using Monolith.WebAPI.Applications.Queries.Vehicles;
using Monolith.WebAPI.Extensions;
using Monolith.WebAPI.Responses.Workspace;
using Swashbuckle.AspNetCore.Annotations;

namespace Monolith.WebAPI.Controllers.Workspace;

[ApiController]
[Route("api/workspace/vehicles")]
[SwaggerControllerOrder(5)]
[Authorize(AuthenticationSchemes = "Bearer")]
public class VehiclesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Get all vehicles of the workspace.")]
    public async Task<IEnumerable<VehicleResponse>> GetVehicles([FromQuery] string? status = null, [FromQuery] string? searchQuery = null)
    {
        // ✅ DÜZELTME: Property initialization ve query parametrelerini ekle
        var query = new GetVehiclesQuery
        {
            AuthenticatedUserId = User.GetId(),
            Status = status,
            SearchQuery = searchQuery
        };
        return await sender.Send(query);
    }

    [HttpGet("{vehicleId:int}")]
    [SwaggerOperation(Summary = "Get a vehicle by id.")]
    public async Task<VehicleResponse> GetVehicle(int vehicleId)
    {
        // ✅ DÜZELTME: Property initialization kullan
        var query = new GetVehicleQuery
        {
            VehicleId = vehicleId,
            AuthenticatedUserId = User.GetId()
        };
        return await sender.Send(query);
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create a new vehicle for the workspace.")]
    public async Task<VehicleResponse> CreateVehicle(CreateVehicleCommand command)
    {
        command.AuthenticatedUserId = User.GetId();
        return await sender.Send(command);
    }

    [HttpPut("{vehicleId:int}")]
    [SwaggerOperation(Summary = "Update a vehicle by id.")]
    public async Task<VehicleResponse> UpdateVehicle(int vehicleId, UpdateVehicleCommand command)
    {
        command.VehicleId = vehicleId;
        command.AuthenticatedUserId = User.GetId();
        return await sender.Send(command);
    }

    [HttpDelete("{vehicleId:int}")]
    [SwaggerOperation(Summary = "Delete a vehicle by id.")]
    public async Task DeleteVehicle(int vehicleId)
    {
        await sender.Send(new DeleteVehicleCommand
        {
            VehicleId = vehicleId,
            AuthenticatedUserId = User.GetId()
        });
    }

    [HttpPut("{vehicleId:int}/status")]
    [SwaggerOperation(Summary = "Update vehicle status.")]
    public async Task<VehicleResponse> UpdateVehicleStatus(int vehicleId, [FromBody] UpdateVehicleStatusCommand command)
    {
        command.VehicleId = vehicleId;
        command.AuthenticatedUserId = User.GetId();
        return await sender.Send(command);
    }
}