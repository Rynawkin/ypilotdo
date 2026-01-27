using System.ComponentModel.DataAnnotations.Schema;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;

namespace Monolith.WebAPI.Data.Workspace
{
    public class Customer : BaseEntity
    {
        public int WorkspaceId { get; set; }
        public Workspace? Workspace { get; set; }
        
        // Basic Information
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        
        // WhatsApp Settings
        public string? WhatsApp { get; set; } // WhatsApp numarası (ülke koduyla birlikte, örn: +905551234567)
        public bool WhatsAppOptIn { get; set; } = false; // Müşteri WhatsApp almayı kabul etti mi
        public bool WhatsAppVerified { get; set; } = false; // Numara doğrulandı mı
        public DateTime? WhatsAppOptInDate { get; set; } // Onay tarihi (KVKK/GDPR için)
        
        // Location
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        
        // Service Settings
        public string Priority { get; set; } = "normal"; // high, normal, low
        public int EstimatedServiceTime { get; set; } = 10; // dakika
        
        // Time Window - Entity Framework için doğru tip ve attribute
        [Column(TypeName = "time")]
        public TimeSpan? TimeWindowStart { get; set; }

        [Column(TypeName = "time")]
        public TimeSpan? TimeWindowEnd { get; set; }
        
        // Additional Information
        public string? Notes { get; set; }
        public string? Tags { get; set; } // Comma-separated tags

        // Delivery Tracking
        public DateTime? LastDeliveryDate { get; set; } // Son teslimat tarihi - performans için denormalize

        // Relations
        public ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();
        public ICollection<CustomerContact> Contacts { get; set; } = new List<CustomerContact>();
    }
}