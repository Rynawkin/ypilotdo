using System.ComponentModel.DataAnnotations;

namespace Monolith.WebAPI.Data.Workspace;

public class PaymentMethodEntity : BaseEntity
{
    public int WorkspaceId { get; set; }
    
    [MaxLength(50)]
    public PaymentMethodType Type { get; set; }
    
    public bool IsDefault { get; set; }
    
    [MaxLength(50)]
    public string? Provider { get; set; } // PayTR, ParamPOS, etc.
    
    [MaxLength(4)]
    public string? LastFourDigits { get; set; }
    
    [MaxLength(100)]
    public string? CardHolderName { get; set; }
    
    [MaxLength(2)]
    public string? ExpiryMonth { get; set; }
    
    [MaxLength(4)]
    public string? ExpiryYear { get; set; }
    
    [MaxLength(50)]
    public string? BrandName { get; set; }
    
    [MaxLength(256)]
    public string? ProviderMethodId { get; set; } // PayTR, ParamPOS method ID
    
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    public Workspace Workspace { get; set; }
}

public enum PaymentMethodType
{
    CreditCard = 0,
    DebitCard = 1,
    BankTransfer = 2
}