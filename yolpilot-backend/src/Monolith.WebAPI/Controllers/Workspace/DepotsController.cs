using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolith.WebAPI.Applications.Commands.Workspace;
using Monolith.WebAPI.Applications.Queries.Workspace;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Data.Workspace; // WorkingHourDto için eklendi
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Monolith.WebAPI.Extensions;

namespace Monolith.WebAPI.Controllers.Workspace;

[ApiController]
[Route("api/workspace/depots")]
[Authorize(AuthenticationSchemes = "Bearer")]
[ApiExplorerSettings(GroupName = "Workspace")]
[SwaggerControllerOrder(30)]
public class DepotsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public DepotsController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    /// <summary>
    /// Tüm depoları listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDepots([FromQuery] string searchTerm = null)
    {
        var query = new GetDepotsQueryWorkspace 
        { 
            SearchTerm = searchTerm,
            AuthenticatedUserId = User.GetId() // GetId kullan
        };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    
    /// <summary>
    /// Belirli bir depoyu getir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDepot(int id)
    {
        var query = new GetDepotQuery 
        { 
            Id = id,
            AuthenticatedUserId = User.GetId() // GetId kullan
        };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    
    /// <summary>
    /// Yeni depo oluştur (Frontend uyumlu)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateDepot([FromBody] CreateDepotDto dto)
    {
        // Frontend'den gelen WorkingHours'u TimeSpan'a çevir
        var (startTime, endTime) = ConvertWorkingHours(dto.WorkingHours);
        
        var command = new CreateDepotCommand
        {
            Name = dto.Name,
            Address = dto.Address,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            StartWorkingHours = startTime,
            EndWorkingHours = endTime,
            AuthenticatedUserId = User.GetId() // GetId kullan
        };
        
        var result = await _mediator.Send(command);
        
        // IsDefault ayarla
        if (dto.IsDefault && result != null)
        {
            var setDefaultCommand = new SetDefaultDepotCommand 
            { 
                Id = result.Id,
                AuthenticatedUserId = User.GetId() // GetId kullan
            };
            result = await _mediator.Send(setDefaultCommand);
        }
        
        return CreatedAtAction(nameof(GetDepot), new { id = result.Id }, result);
    }
    
    /// <summary>
    /// Depoyu güncelle (Frontend uyumlu)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDepot(int id, [FromBody] UpdateDepotDto dto)
    {
        // Frontend'den gelen WorkingHours'u TimeSpan'a çevir
        var (startTime, endTime) = ConvertWorkingHours(dto.WorkingHours);
        
        var command = new UpdateDepotCommand
        {
            DepotId = id,
            Name = dto.Name,
            Address = dto.Address,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            StartWorkingHours = startTime,
            EndWorkingHours = endTime,
            AuthenticatedUserId = User.GetId() // GetId kullan
        };
        
        var result = await _mediator.Send(command);
        
        // IsDefault ayarla
        if (dto.IsDefault)
        {
            var setDefaultCommand = new SetDefaultDepotCommand 
            { 
                Id = id,
                AuthenticatedUserId = User.GetId() // GetId kullan
            };
            result = await _mediator.Send(setDefaultCommand);
        }
        
        return Ok(result);
    }
    
    /// <summary>
    /// Depoyu sil
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDepot(int id)
    {
        var command = new DeleteDepotCommand 
        { 
            Id = id,
            AuthenticatedUserId = User.GetId() // GetId kullan
        };
        await _mediator.Send(command);
        return NoContent();
    }
    
    /// <summary>
    /// Depoyu ana depo olarak ayarla
    /// </summary>
    [HttpPost("{id}/set-default")]
    public async Task<IActionResult> SetDefaultDepot(int id)
    {
        var command = new SetDefaultDepotCommand 
        { 
            Id = id,
            AuthenticatedUserId = User.GetId() // GetId kullan
        };
        var result = await _mediator.Send(command);
        return Ok(result);
    }
    
    // Helper method to convert WorkingHours dictionary to TimeSpan
    private (TimeSpan start, TimeSpan end) ConvertWorkingHours(Dictionary<string, WorkingHourDto> workingHours)
    {
        if (workingHours == null || !workingHours.Any())
            return (TimeSpan.FromHours(8), TimeSpan.FromHours(18));
        
        // Pazartesi'den çalışma saatlerini al (veya ilk açık gün)
        var openDay = workingHours.FirstOrDefault(x => 
            x.Value.Open != "closed" && 
            (x.Key == "monday" || x.Key == "tuesday" || x.Key == "wednesday" || 
             x.Key == "thursday" || x.Key == "friday"));
        
        if (openDay.Value == null)
            return (TimeSpan.FromHours(8), TimeSpan.FromHours(18));
        
        var startTime = TimeSpan.Parse(openDay.Value.Open);
        var endTime = TimeSpan.Parse(openDay.Value.Close);
        
        return (startTime, endTime);
    }
}

// DTO'lar
public class CreateDepotDto
{
    public string Name { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsDefault { get; set; }
    public Dictionary<string, WorkingHourDto> WorkingHours { get; set; }
}

public class UpdateDepotDto
{
    public string Name { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsDefault { get; set; }
    public Dictionary<string, WorkingHourDto> WorkingHours { get; set; }
}