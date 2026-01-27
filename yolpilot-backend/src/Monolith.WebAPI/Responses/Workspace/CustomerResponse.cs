// src/Monolith.WebAPI/Responses/Workspace/CustomerResponse.cs

using Monolith.WebAPI.Applications.Commands.Customers;

namespace Monolith.WebAPI.Responses.Workspace
{
    public class CustomerResponse
    {
        public int Id { get; set; }
        public int WorkspaceId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        
        // WhatsApp Alanları - YENİ EKLENEN
        public string? WhatsApp { get; set; }
        public bool WhatsAppOptIn { get; set; }
        public bool WhatsAppVerified { get; set; }
        public DateTime? WhatsAppOptInDate { get; set; }
        
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Priority { get; set; } = "normal";
        public int EstimatedServiceTime { get; set; } = 10;
        public string? Notes { get; set; }
        public List<string>? Tags { get; set; }
        public TimeWindowDto? TimeWindow { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastDeliveryDate { get; set; } // Son teslimat tarihi

        // Ek bilgiler için metadata (route sayısı, journey sayısı vb.)
        public Dictionary<string, object>? Metadata { get; set; }
    }
}