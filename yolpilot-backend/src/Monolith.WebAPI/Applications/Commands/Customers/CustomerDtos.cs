// src/Monolith.WebAPI/Applications/Commands/Customers/CustomerDtos.cs

using System.Linq;

namespace Monolith.WebAPI.Applications.Commands.Customers
{
    // Merkezi TimeWindowDto tanımı - Tüm projede bu kullanılacak
    public class TimeWindowDto
    {
        public string Start { get; set; } = string.Empty;
        public string End { get; set; } = string.Empty;
    }

    public class CustomerDto
    {
        public string Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; }
        
        // WhatsApp Alanları - YENİ EKLENEN
        public string WhatsApp { get; set; }
        public bool WhatsAppOptIn { get; set; } = false;
        public bool WhatsAppVerified { get; set; } = false;
        public DateTime? WhatsAppOptInDate { get; set; }
        
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Priority { get; set; } = "normal";
        public int EstimatedServiceTime { get; set; } = 10;
        public string Notes { get; set; }
        public List<string> Tags { get; set; }
        public TimeWindowDto TimeWindow { get; set; }
    }

    // Bulk import için gerekli DTO'lar
    public class CustomerImportDto
    {
        public string Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; }
        
        // WhatsApp Alanları - YENİ EKLENEN
        public string WhatsApp { get; set; }
        public bool WhatsAppOptIn { get; set; } = false;
        public bool WhatsAppVerified { get; set; } = false;
        public DateTime? WhatsAppOptInDate { get; set; }
        
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Priority { get; set; } = "normal";
        public int EstimatedServiceTime { get; set; } = 10;
        public string Notes { get; set; }
        public List<string> Tags { get; set; }
        public TimeWindowDto TimeWindow { get; set; }
    }

    public class BulkImportResponse
    {
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public int FailedCount => FailureCount; // Alias for compatibility
        public List<ImportResult> Results { get; set; } = new List<ImportResult>();
        public List<ImportResult> ImportedCustomers => Results.Where(r => r.Success).ToList();
        public List<string> Errors => Results.Where(r => !r.Success).Select(r => r.Error ?? "Unknown error").ToList();
        public string Message { get; set; } = string.Empty;
    }

    public class ImportResult
    {
        public int RowIndex { get; set; }
        public bool Success { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Error { get; set; }
        public int? CreatedId { get; set; }
    }
}
