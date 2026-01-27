// src/Monolith.WebAPI/Controllers/IssuesController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Extensions;

namespace Monolith.WebAPI.Controllers.Workspace
{
    [ApiController]
    [Route("api/workspace/issues")]
    [Authorize]
    public class IssuesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<IssuesController> _logger;

        public IssuesController(AppDbContext context, ILogger<IssuesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("report")]
        public async Task<IActionResult> ReportIssue([FromBody] ReportIssueRequest request)
        {
            var workspaceId = User.GetWorkspaceId();
            
            var issue = new Issue
            {
                IssueType = request.IssueType,
                Subject = request.Subject,
                Description = request.Description,
                Status = "Open",
                Priority = DeterminePriority(request.IssueType),
                ReportedBy = request.ReportedBy,
                ReportedByName = request.ReportedByName,
                WorkspaceId = workspaceId,
                DeviceInfo = request.DeviceInfo
                // CreatedDate kaldırıldı - BaseEntity'den otomatik gelecek
            };

            _context.Issues.Add(issue);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"New issue reported: {issue.Subject} by {issue.ReportedBy}");

            return Ok(new { message = "Sorun bildirimi alındı", issueId = issue.Id });
        }

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetIssues([FromQuery] string? status = null)
        {
            var workspaceId = User.GetWorkspaceId();
            var isSuperAdmin = User.IsInRole("SuperAdmin");

            var query = _context.Issues.Include(i => i.Workspace).AsQueryable();

            if (!isSuperAdmin)
            {
                query = query.Where(i => i.WorkspaceId == workspaceId);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(i => i.Status == status);
            }

            var issues = await query
                .OrderByDescending(i => i.Id) // CreatedDate yerine Id kullanıyoruz
                .Select(i => new IssueResponse
                {
                    Id = i.Id,
                    IssueType = i.IssueType,
                    Subject = i.Subject,
                    Description = i.Description,
                    Status = i.Status,
                    Priority = i.Priority,
                    ReportedBy = i.ReportedBy,
                    ReportedByName = i.ReportedByName,
                    WorkspaceId = i.WorkspaceId,
                    WorkspaceName = i.Workspace.Name,
                    DeviceInfo = i.DeviceInfo,
                    AdminNotes = i.AdminNotes,
                    CreatedAt = i.CreatedAt, // BaseEntity'deki property ismi
                    ResolvedAt = i.ResolvedAt
                })
                .ToListAsync();

            return Ok(issues);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateIssueStatus(int id, [FromBody] UpdateIssueStatusRequest request)
        {
            var issue = await _context.Issues.FindAsync(id);
            if (issue == null)
                return NotFound();

            issue.Status = request.Status;
            issue.AdminNotes = request.AdminNotes;
            
            if (request.Status == "Resolved" || request.Status == "Closed")
            {
                issue.ResolvedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Durum güncellendi" });
        }

        private string DeterminePriority(string issueType)
        {
            return issueType switch
            {
                "bug" => "High",
                "performance" => "Medium",
                "feature" => "Low",
                _ => "Low"
            };
        }
    }

    public class ReportIssueRequest
    {
        public string IssueType { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string ReportedBy { get; set; }
        public string ReportedByName { get; set; }
        public int WorkspaceId { get; set; }
        public string? DeviceInfo { get; set; }
    }

    public class UpdateIssueStatusRequest
    {
        public string Status { get; set; }
        public string? AdminNotes { get; set; }
    }

    public class IssueResponse
    {
        public int Id { get; set; }
        public string IssueType { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public string ReportedBy { get; set; }
        public string ReportedByName { get; set; }
        public int WorkspaceId { get; set; }
        public string WorkspaceName { get; set; }
        public string? DeviceInfo { get; set; }
        public string? AdminNotes { get; set; }
        public DateTime CreatedAt { get; set; } // CreatedDate yerine CreatedAt
        public DateTime? ResolvedAt { get; set; }
    }
}