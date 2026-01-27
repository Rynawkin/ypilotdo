// src/Monolith.WebAPI/Controllers/Workspace/DriversController.cs

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolith.WebAPI.Applications.Commands.Drivers;
using Monolith.WebAPI.Applications.Queries.Drivers;
using Monolith.WebAPI.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Monolith.WebAPI.Controllers.Workspace;

[ApiController]
[Route("api/workspace/drivers")] // DÜZELTME: api prefix eklendi
[SwaggerControllerOrder(4)] // Swagger sıralaması
[Authorize(AuthenticationSchemes = "Bearer")] // DÜZELTME: Bearer authentication
public class DriversController(ISender sender) : ControllerBase // DÜZELTME: Primary constructor
{
    [HttpGet]
    [SwaggerOperation(Summary = "Get all drivers in the workspace")]
    public async Task<IActionResult> GetDrivers(
        [FromQuery] string? status = null, 
        [FromQuery] string? search = null)
    {
        var query = new GetDriversQuery
        {
            AuthenticatedUserId = User.GetId(), // Extension method kullan
            Status = status,
            SearchQuery = search
        };

        var result = await sender.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get a specific driver by ID")]
    public async Task<IActionResult> GetDriver(int id)
    {
        var query = new GetDriverQuery
        {
            AuthenticatedUserId = User.GetId(),
            Id = id
        };

        try
        {
            var result = await sender.Send(query);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("available")]
    [SwaggerOperation(Summary = "Get all available drivers")]
    public async Task<IActionResult> GetAvailableDrivers()
    {
        var query = new GetAvailableDriversQuery
        {
            AuthenticatedUserId = User.GetId()
        };

        var result = await sender.Send(query);
        return Ok(result);
    }

    [HttpGet("search")]
    [SwaggerOperation(Summary = "Search drivers")]
    public async Task<IActionResult> SearchDrivers([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Ok(new List<Responses.Workspace.DriverResponse>());
        }

        var searchQuery = new SearchDriversQuery
        {
            AuthenticatedUserId = User.GetId(),
            Query = query
        };

        var result = await sender.Send(searchQuery);
        return Ok(result);
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create a new driver")]
    public async Task<IActionResult> CreateDriver([FromBody] CreateDriverDto dto)
    {
        var command = new CreateDriverCommand
        {
            AuthenticatedUserId = User.GetId(),
            Data = dto
        };

        try
        {
            var result = await sender.Send(command);
            return CreatedAtAction(nameof(GetDriver), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [SwaggerOperation(Summary = "Update an existing driver")]
    public async Task<IActionResult> UpdateDriver(int id, [FromBody] UpdateDriverDto dto)
    {
        var command = new UpdateDriverCommand
        {
            AuthenticatedUserId = User.GetId(),
            Id = id,
            Data = dto
        };

        try
        {
            var result = await sender.Send(command);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/status")]
    [SwaggerOperation(Summary = "Update driver status")]
    public async Task<IActionResult> UpdateDriverStatus(int id, [FromBody] UpdateDriverStatusDto dto)
    {
        var command = new UpdateDriverStatusCommand
        {
            AuthenticatedUserId = User.GetId(),
            Id = id,
            Status = dto.Status
        };

        try
        {
            var result = await sender.Send(command);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [SwaggerOperation(Summary = "Delete a driver")]
    public async Task<IActionResult> DeleteDriver(int id)
    {
        var command = new DeleteDriverCommand
        {
            AuthenticatedUserId = User.GetId(),
            Id = id
        };

        try
        {
            await sender.Send(command);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("bulk")]
    [SwaggerOperation(Summary = "Bulk import drivers")]
    public async Task<IActionResult> BulkImportDrivers([FromBody] List<BulkImportDriverDto> drivers)
    {
        if (drivers == null || !drivers.Any())
        {
            return BadRequest(new { message = "Sürücü listesi boş olamaz" });
        }

        if (drivers.Count > 100)
        {
            return BadRequest(new { message = "Tek seferde en fazla 100 sürücü eklenebilir" });
        }

        var command = new BulkImportDriversCommand
        {
            AuthenticatedUserId = User.GetId(),
            Drivers = drivers
        };

        var result = await sender.Send(command);
        return Ok(result);
    }
}