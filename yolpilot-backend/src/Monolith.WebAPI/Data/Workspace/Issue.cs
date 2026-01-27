// src/Monolith.WebAPI/Data/Workspace/Issue.cs

using System;

namespace Monolith.WebAPI.Data.Workspace
{
    public class Issue : BaseEntity
    {
        public string IssueType { get; set; } // bug, feature, performance, other
        public string Subject { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } // Open, InProgress, Resolved, Closed
        public string Priority { get; set; } // Low, Medium, High, Critical
        public string ReportedBy { get; set; }
        public string ReportedByName { get; set; }
        public int WorkspaceId { get; set; }
        public string? DeviceInfo { get; set; }
        public string? AdminNotes { get; set; }
        public DateTime? ResolvedAt { get; set; }
        
        public virtual Workspace Workspace { get; set; }
    }
}