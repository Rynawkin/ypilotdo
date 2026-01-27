using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Extensions;
using Monolith.WebAPI.Services.Templates;
using System.Text.Json;

namespace Monolith.WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/workspace/templates")]
    public class TemplatesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITemplateService _templateService;

        public TemplatesController(AppDbContext context, ITemplateService templateService)
        {
            _context = context;
            _templateService = templateService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTemplates()
        {
            var workspaceId = User.GetWorkspaceId();
            var templates = await _templateService.GetWorkspaceTemplatesAsync(workspaceId);
            
            return Ok(templates.Select(t => new
            {
                t.Id,
                t.TemplateType,
                t.Channel,
                t.Name,
                t.Subject,
                t.Body,
                Variables = string.IsNullOrEmpty(t.Variables) ? new string[0] : JsonSerializer.Deserialize<string[]>(t.Variables),
                t.IsActive,
                t.IsDefault,
                t.CreatedAt,
                t.UpdatedAt
            }));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTemplate(int id)
        {
            var workspaceId = User.GetWorkspaceId();
            var template = await _context.MessageTemplates
                .FirstOrDefaultAsync(t => t.Id == id && t.WorkspaceId == workspaceId);
            
            if (template == null)
                return NotFound();
            
            return Ok(new
            {
                template.Id,
                template.TemplateType,
                template.Channel,
                template.Name,
                template.Subject,
                template.Body,
                Variables = string.IsNullOrEmpty(template.Variables) ? new string[0] : JsonSerializer.Deserialize<string[]>(template.Variables),
                template.IsActive,
                template.IsDefault,
                template.CreatedAt,
                template.UpdatedAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request)
        {
            var workspaceId = User.GetWorkspaceId();
            var userId = User.GetUserId();
            
            // Eğer yeni template aktifse, aynı tip ve kanaldaki diğer template'leri pasif yap
            if (request.IsActive)
            {
                var existingActiveTemplates = await _context.MessageTemplates
                    .Where(t => t.WorkspaceId == workspaceId 
                             && t.TemplateType == request.TemplateType 
                             && t.Channel == request.Channel
                             && t.IsActive)
                    .ToListAsync();

                foreach (var existingTemplate in existingActiveTemplates)
                {
                    existingTemplate.IsActive = false;
                    existingTemplate.UpdatedAt = DateTime.UtcNow;
                }
            }
            
            var template = new MessageTemplate
            {
                WorkspaceId = workspaceId,
                TemplateType = request.TemplateType,
                Channel = request.Channel,
                Name = request.Name,
                Subject = request.Subject,
                Body = request.Body,
                Variables = JsonSerializer.Serialize(request.Variables ?? new string[0]),
                IsActive = request.IsActive,
                IsDefault = false,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.MessageTemplates.Add(template);
            await _context.SaveChangesAsync();
            
            return Ok(new
            {
                template.Id,
                template.TemplateType,
                template.Channel,
                template.Name,
                template.Subject,
                template.Body,
                Variables = JsonSerializer.Deserialize<string[]>(template.Variables),
                template.IsActive,
                template.IsDefault
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTemplate(int id, [FromBody] UpdateTemplateRequest request)
        {
            var workspaceId = User.GetWorkspaceId();
            var template = await _context.MessageTemplates
                .FirstOrDefaultAsync(t => t.Id == id && t.WorkspaceId == workspaceId && !t.IsDefault);
            
            if (template == null)
                return NotFound("Template not found or cannot be edited");

            // Eğer template aktif ediliyorsa, aynı tip ve kanaldaki diğerleri pasif yap
            if (request.IsActive && !template.IsActive)
            {
                var otherActiveTemplates = await _context.MessageTemplates
                    .Where(t => t.WorkspaceId == workspaceId 
                             && t.TemplateType == template.TemplateType 
                             && t.Channel == template.Channel
                             && t.Id != id
                             && t.IsActive)
                    .ToListAsync();

                foreach (var otherTemplate in otherActiveTemplates)
                {
                    otherTemplate.IsActive = false;
                    otherTemplate.UpdatedAt = DateTime.UtcNow;
                }
            }

            template.Name = request.Name;
            template.Subject = request.Subject;
            template.Body = request.Body;
            template.Variables = JsonSerializer.Serialize(request.Variables ?? new string[0]);
            template.IsActive = request.IsActive;
            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            return Ok(new
            {
                template.Id,
                template.TemplateType,
                template.Channel,
                template.Name,
                template.Subject,
                template.Body,
                Variables = JsonSerializer.Deserialize<string[]>(template.Variables),
                template.IsActive,
                template.IsDefault
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var workspaceId = User.GetWorkspaceId();
            var deleted = await _templateService.DeleteTemplateAsync(id, workspaceId);
            
            if (!deleted)
                return NotFound("Template not found or cannot be deleted");
            
            return NoContent();
        }

        [HttpPost("{id}/preview")]
        public async Task<IActionResult> PreviewTemplate(int id, [FromBody] PreviewTemplateRequest request)
        {
            var workspaceId = User.GetWorkspaceId();
            var template = await _context.MessageTemplates
                .FirstOrDefaultAsync(t => t.Id == id && t.WorkspaceId == workspaceId);
            
            if (template == null)
                return NotFound();

            var processedSubject = await _templateService.ProcessTemplateAsync(template.Subject, request.SampleData);
            var processedBody = await _templateService.ProcessTemplateAsync(template.Body, request.SampleData);

            return Ok(new
            {
                subject = processedSubject,
                body = processedBody
            });
        }

        [HttpGet("variables")]
        public IActionResult GetAvailableVariables()
        {
            var variables = new Dictionary<string, string[]>
            {
                ["WelcomeEmail"] = new[] { "user.fullName", "user.email", "workspace.name", "workspace.email", "workspace.phoneNumber", "loginUrl" },
                ["JourneyStart"] = new[] { "customer.name", "journey.date", "driver.name", "driver.phone", "vehicle.brand", "vehicle.model", "vehicle.plateNumber", "estimatedCompletionTime", "trackingUrl", "workspace.name" },
                ["CheckIn"] = new[] { "customer.name", "customer.address", "stop.estimatedArrivalTime", "driver.name", "driver.phone", "trackingUrl", "workspace.name" },
                ["DeliveryCompleted"] = new[] { "customer.name", "customer.address", "completedTime", "driver.name", "signatureUrl", "photoUrl", "stop.notes", "feedbackUrl", "trackingUrl", "receiverName", "workspace.name" },
                ["DeliveryFailed"] = new[] { "customer.name", "customer.address", "failureReason", "failureTime", "driver.name", "driver.phone", "rescheduleUrl", "workspace.name" }
            };

            return Ok(variables);
        }
    }

    public class CreateTemplateRequest
    {
        public string TemplateType { get; set; }
        public string Channel { get; set; }
        public string Name { get; set; }
        public string? Subject { get; set; }
        public string Body { get; set; }
        public string[]? Variables { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateTemplateRequest
    {
        public string Name { get; set; }
        public string? Subject { get; set; }
        public string Body { get; set; }
        public string[]? Variables { get; set; }
        public bool IsActive { get; set; }
    }

    public class PreviewTemplateRequest
    {
        public Dictionary<string, object> SampleData { get; set; }
    }
}