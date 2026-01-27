using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monolith.WebAPI.Data.Workspace;

public class PaymentTransaction : BaseEntity
{
    public int WorkspaceId { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    [MaxLength(3)]
    public string Currency { get; set; } = "TRY";
    
    [MaxLength(50)]
    public PaymentStatus Status { get; set; }
    
    public PaymentMethod PaymentMethod { get; set; }
    
    [MaxLength(256)]
    public string? ProviderTransactionId { get; set; }
    
    [Column(TypeName = "nvarchar(max)")]
    public string? ProviderResponse { get; set; }
    
    [MaxLength(50)]
    public string? Provider { get; set; } // PayTR, ParamPOS, etc.
    
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }
    
    public DateTime? ProcessedAt { get; set; }
    
    // Navigation Properties
    public Workspace Workspace { get; set; }
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

public enum PaymentStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    Refunded = 5
}

public enum PaymentMethod
{
    CreditCard = 0,
    BankTransfer = 1,
    PayTR = 2,
    ParamPOS = 3
}