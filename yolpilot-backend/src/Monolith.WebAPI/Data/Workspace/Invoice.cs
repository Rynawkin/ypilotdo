using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Monolith.WebAPI.Data.Workspace;

public class Invoice : BaseEntity
{
    public int WorkspaceId { get; set; }
    
    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Tax { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }
    
    public DateTime DueDate { get; set; }
    
    public DateTime? PaidDate { get; set; }
    
    [MaxLength(50)]
    public InvoiceStatus Status { get; set; }
    
    [MaxLength(50)]
    public PlanType PlanType { get; set; }
    
    public DateTime PeriodStart { get; set; }
    
    public DateTime PeriodEnd { get; set; }
    
    public List<InvoiceItem>? Items { get; set; } // JSON format: [{"description": "Starter Plan", "amount": 850, "quantity": 1}]
    
    public int? PaymentTransactionId { get; set; }
    
    // Navigation Properties
    public Workspace Workspace { get; set; }
    public PaymentTransaction? PaymentTransaction { get; set; }
}

public enum InvoiceStatus
{
    Draft = 0,
    Pending = 1,
    Paid = 2,
    Overdue = 3,
    Cancelled = 4,
    Refunded = 5
}