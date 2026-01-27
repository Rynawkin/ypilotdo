using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands.Journeys;
using Monolith.WebAPI.Applications.Queries.Journeys;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Extensions;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Journeys;
using Swashbuckle.AspNetCore.Annotations;

namespace Monolith.WebAPI.Controllers.Workspace;

[Route("api/workspace/routes")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
[SwaggerControllerOrder(7)]
public class RoutesController(ISender sender, AppDbContext context) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [HttpGet]
    [SwaggerOperation(Summary = "Get all routes in the workspace")]
    public async Task<IEnumerable<RouteResponse>> GetRoutes()
    {
        var query = new GetRoutesQuery(User.GetWorkspaceId());
        return await sender.Send(query);
    }

    [HttpGet("{routeId:int}")]
    [SwaggerOperation(Summary = "Get a route by id")]
    public async Task<RouteResponse> GetRoute(int routeId)
    {
        var query = new GetRouteQuery(User.GetWorkspaceId(), routeId);
        return await sender.Send(query);
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create a new route")]
    public async Task<RouteResponse> CreateRoute(CreateRouteCommand command)
    {
        command.AuthenticatedUserId = User.GetId();
        return await sender.Send(command);
    }

    [HttpPut("{routeId:int}")]
    [SwaggerOperation(Summary = "Update a route")]
    public async Task<RouteResponse> UpdateRoute(int routeId, UpdateRouteCommand command)
    {
        command.RouteId = routeId;
        command.AuthenticatedUserId = User.GetId();
        return await sender.Send(command);
    }

    [HttpDelete("{routeId:int}")]
    [SwaggerOperation(Summary = "Delete a route")]
    public async Task<IActionResult> DeleteRoute(int routeId)
    {
        var command = new DeleteRouteCommand
        {
            RouteId = routeId,
            AuthenticatedUserId = User.GetId()
        };
        await sender.Send(command);
        return NoContent();
    }

    [HttpPost("{routeId:int}/optimize")]
    [SwaggerOperation(Summary = "Optimize a route using Google Maps API")]
    public async Task<IActionResult> OptimizeRoute(int routeId, [FromBody] OptimizeRouteCommand command = null)
    {
        if (command == null)
        {
            command = new OptimizeRouteCommand();
        }
        command.RouteId = routeId;
        command.AuthenticatedUserId = User.GetId();

        var workspaceId = User.GetWorkspaceId();
        var routeExists = await context.Routes
            .AsNoTracking()
            .AnyAsync(r => r.Id == routeId && r.WorkspaceId == workspaceId && !r.IsDeleted);

        if (!routeExists)
        {
            throw new ApiException("Route not found.", 404);
        }

        var job = new RouteOptimizationJob
        {
            PublicId = Guid.NewGuid(),
            RouteId = routeId,
            WorkspaceId = workspaceId,
            RequestedByUserId = User.GetId(),
            OptimizationMode = command.OptimizationMode ?? "distance",
            AvoidTolls = command.AvoidTolls,
            PreserveOrder = command.PreserveOrder,
            IsTimeDeviationOptimization = command.IsTimeDeviationOptimization,
            Status = RouteOptimizationJobStatus.Pending,
            Message = "Optimization queued."
        };

        context.RouteOptimizationJobs.Add(job);
        await context.SaveChangesAsync();

        var response = new RouteOptimizationJobResponse
        {
            JobId = job.PublicId,
            RouteId = job.RouteId,
            Status = job.Status,
            Message = job.Message
        };

        return AcceptedAtAction(nameof(GetOptimizationJob), new { jobId = job.PublicId }, response);
    }

    [HttpGet("optimization-jobs/{jobId:guid}")]
    [SwaggerOperation(Summary = "Get optimization job status")]
    public async Task<RouteOptimizationJobStatusResponse> GetOptimizationJob(Guid jobId)
    {
        var workspaceId = User.GetWorkspaceId();
        var job = await context.RouteOptimizationJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.PublicId == jobId && j.WorkspaceId == workspaceId);

        if (job == null)
        {
            throw new ApiException("Optimization job not found.", 404);
        }

        JsonElement? result = null;
        if (!string.IsNullOrWhiteSpace(job.ResultJson))
        {
            try
            {
                result = JsonSerializer.Deserialize<JsonElement>(job.ResultJson, JsonOptions);
            }
            catch
            {
                // If stored JSON is invalid or incompatible, return null result but keep status/message.
                result = null;
            }
        }

        return new RouteOptimizationJobStatusResponse
        {
            JobId = job.PublicId,
            RouteId = job.RouteId,
            Status = job.Status,
            Message = job.Message ?? string.Empty,
            Result = result
        };
    }
}
