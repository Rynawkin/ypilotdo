using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolith.WebAPI.Applications.Commands.Journeys;
using Monolith.WebAPI.Applications.Queries.Journeys;
using Monolith.WebAPI.Extensions;
using Monolith.WebAPI.Requests;
using Monolith.WebAPI.Responses.Journeys;
using Swashbuckle.AspNetCore.Annotations;

namespace Monolith.WebAPI.Controllers.Journeys;

[Route("routes")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
[SwaggerControllerOrder(5)]
public class RoutesController(ISender sender) : ControllerBase
{
    [SwaggerOperation(Summary = "Get all routes of the workspace.")]
    [HttpGet]
    public async Task<IEnumerable<RouteResponse>> GetRoutes()
    {
        var query = new GetRoutesQuery(User.GetWorkspaceId());
        return await sender.Send(query);
    }

    [SwaggerOperation(Summary = "Get a route by id.")]
    [HttpGet("{routeId:int}")]
    public async Task<RouteResponse> GetRoute(int routeId)
    {
        var query = new GetRouteQuery(User.GetWorkspaceId(), routeId);
        return await sender.Send(query);
    }

    [SwaggerOperation(Summary = "Create a new route, no need to give start and end details, the route depot will be used.")]
    [HttpPost]
    public async Task<RouteResponse> CreateRoute(CreateRouteCommand command)
    {
        command.AuthenticatedUserId = User.GetId();
        var route = await sender.Send(command);
        return route;
    }

    [SwaggerOperation(Summary = "Add a stop to the route.")]
    [HttpPost("{routeId:int}/stops")]
    public async Task<IEnumerable<RouteStopResponse>> AddStop(int routeId, List<RouteStopRequest> stops)
    {
        var command = new AddRouteStopsCommand
        {
            RouteId = routeId,
            AuthenticatedUserId = User.GetId(),
            Stops = stops
        };
        return await sender.Send(command);
    }

    [SwaggerOperation(Summary = "Update a stop.")]
    [HttpPut("{routeId:int}/stops/{stopId:int}")]
    public async Task<bool> UpdateStop(int routeId, int stopId, UpdateStopCommand command)
    {
        command.RouteId = routeId;
        command.StopId = stopId;
        command.AuthenticatedUserId = User.GetId();
        return await sender.Send(command);
    }

    [SwaggerOperation(Summary = "Delete a stop.")]
    [HttpDelete("{routeId:int}/stops/{stopId:int}")]
    public async Task DeleteStop(int routeId, int stopId)
    {
        var command = new DeleteStopCommand
        {
            RouteId = routeId,
            StopId = stopId,
            AuthenticatedUserId = User.GetId()
        };
        await sender.Send(command);
    }
    
    [SwaggerOperation(Summary = "Optimize route stops for shortest distance or duration.")]
    [HttpPost("{routeId:int}/optimize")]
    public async Task<OptimizeRouteResponse> OptimizeRoute(int routeId, [FromBody] OptimizeRouteRequest request)
    {
        var command = new OptimizeRouteCommand
        {
            RouteId = routeId,
            OptimizationMode = request?.OptimizationMode ?? "distance",
            AvoidTolls = request?.AvoidTolls ?? false,
            AuthenticatedUserId = User.GetId()
        };
        return await sender.Send(command);
    }
}