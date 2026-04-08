using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Services.Payment;

public interface IPaymentProvider
{
    string ProviderName { get; }
    
    Task<PaymentResult> InitiatePaymentAsync(PaymentRequest request);
    Task<PaymentResult> ProcessWebhookAsync(string payload, Dictionary<string, string> headers);
    Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount);
    Task<PaymentResult> GetPaymentStatusAsync(string transactionId);
    Task<StoredPaymentMethodResult> StorePaymentMethodAsync(StorePaymentMethodRequest request);
    Task<PaymentResult> ChargeStoredPaymentMethodAsync(StoredPaymentChargeRequest request);
}

public class PaymentRequest
{
    public int WorkspaceId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Description { get; set; } = string.Empty;
    public string? CallbackUrl { get; set; }
    public string? SuccessUrl { get; set; }
    public string? FailUrl { get; set; }
    public string? ClientIp { get; set; }
    public string? ReferrerUrl { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public PaymentCard? Card { get; set; }
    public PlanType? PlanType { get; set; }
    public Dictionary<string, object> ExtraData { get; set; } = new();
}

public class PaymentCard
{
    public string CardHolderName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public string ExpiryMonth { get; set; } = string.Empty;
    public string ExpiryYear { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
    public string? CardHolderPhone { get; set; }
}

public class PaymentResult
{
    public bool IsSuccess { get; set; }
    public string? TransactionId { get; set; }
    public int? InternalTransactionId { get; set; }
    public string? PaymentUrl { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public Dictionary<string, object> ProviderData { get; set; } = new();
    public DateTime? ProcessedAt { get; set; }
}

public class StorePaymentMethodRequest
{
    public int WorkspaceId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public PaymentCard Card { get; set; } = new();
    public string? Alias { get; set; }
    public string? ExternalReference { get; set; }
}

public class StoredPaymentChargeRequest
{
    public int WorkspaceId { get; set; }
    public string ProviderMethodId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Description { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? ClientIp { get; set; }
    public string? ReferrerUrl { get; set; }
    public PlanType? PlanType { get; set; }
    public Dictionary<string, object> ExtraData { get; set; } = new();
}

public class StoredPaymentMethodResult
{
    public bool IsSuccess { get; set; }
    public string? ProviderMethodId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? LastFourDigits { get; set; }
    public string? CardHolderName { get; set; }
    public string? ExpiryMonth { get; set; }
    public string? ExpiryYear { get; set; }
    public string? BrandName { get; set; }
    public Dictionary<string, object> ProviderData { get; set; } = new();
}
