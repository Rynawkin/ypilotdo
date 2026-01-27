using System;
using System.ComponentModel.DataAnnotations;

namespace Monolith.WebAPI.Data.Workspace
{
    public class MessageTemplate : BaseEntity
    {
        [Required]
        public int WorkspaceId { get; set; }
        public virtual Workspace Workspace { get; set; }

        [Required]
        [MaxLength(50)]
        public string TemplateType { get; set; } // WelcomeEmail, JourneyStart, CheckIn, DeliveryCompleted, DeliveryFailed

        [Required]
        [MaxLength(20)]
        public string Channel { get; set; } // Email, WhatsApp

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(200)]
        public string Subject { get; set; } // Email için

        [Required]
        public string Body { get; set; }

        public string Variables { get; set; } // JSON array

        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;

        public Guid? CreatedBy { get; set; }
    }

    public enum TemplateType
    {
        WelcomeEmail,
        JourneyStart,
        CheckIn,
        DeliveryCompleted,
        DeliveryFailed
    }

    public enum TemplateChannel
    {
        Email,
        WhatsApp
    }
}