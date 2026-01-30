using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Services.Payment;

public interface IPaymentService
{
    Task<PaymentResult> InitiatePaymentAsync(PaymentRequest request);
    Task<PaymentResult> ProcessWebhookAsync(string provider, string payload, Dictionary<string, string> headers);
    Task<PaymentTransaction> CreatePaymentTransactionAsync(PaymentRequest request, PaymentResult result);
    Task UpdatePaymentTransactionAsync(string transactionId, PaymentResult result);
    Task<List<Invoice>> GetInvoicesAsync(int workspaceId);
    Task<Invoice> CreateInvoiceAsync(int workspaceId, PlanType planType, decimal amount, int? paymentTransactionId = null);
}

public class PaymentService : IPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;
    private readonly AppDbContext _context;
    private readonly Dictionary<string, IPaymentProvider> _providers;

    public PaymentService(
        IConfiguration configuration,
        ILogger<PaymentService> logger,
        AppDbContext context,
        IEnumerable<IPaymentProvider> providers)
    {
        _configuration = configuration;
        _logger = logger;
        _context = context;
        _providers = providers.ToDictionary(p => p.ProviderName.ToLower(), p => p);
    }

    public async Task<PaymentResult> InitiatePaymentAsync(PaymentRequest request)
    {
        var providerName = _configuration["Payment:Provider"]?.ToLower() ?? "paytr";
        
        if (!_providers.TryGetValue(providerName, out var provider))
        {
            _logger.LogError("Payment provider {Provider} not found", providerName);
            return new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = "Payment provider not available"
            };
        }

        var result = await provider.InitiatePaymentAsync(request);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Payment initiation failed via {Provider}: {Error}", providerName, result.ErrorMessage);
        }

        if (result.IsSuccess)
        {
            EnsureProviderDataDefaults(request, result);
            var transaction = await CreatePaymentTransactionAsync(request, result);
            result.InternalTransactionId = transaction.Id;
        }

        return result;
    }

    public async Task<PaymentResult> ProcessWebhookAsync(string provider, string payload, Dictionary<string, string> headers)
    {
        if (!_providers.TryGetValue(provider.ToLower(), out var paymentProvider))
        {
            _logger.LogError("Payment provider {Provider} not found for webhook", provider);
            return new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = "Provider not found"
            };
        }

        var result = await paymentProvider.ProcessWebhookAsync(payload, headers);
        
        if (result.IsSuccess && !string.IsNullOrEmpty(result.TransactionId))
        {
            await UpdatePaymentTransactionAsync(result.TransactionId, result);
        }

        return result;
    }

    public async Task<PaymentTransaction> CreatePaymentTransactionAsync(PaymentRequest request, PaymentResult result)
    {
        var transaction = new PaymentTransaction
        {
            WorkspaceId = request.WorkspaceId,
            Amount = request.Amount,
            Currency = request.Currency,
            Status = result.Status,
            PaymentMethod = PaymentMethod.CreditCard, // Default
            ProviderTransactionId = result.TransactionId,
            Provider = _configuration["Payment:Provider"],
            ProviderResponse = System.Text.Json.JsonSerializer.Serialize(result.ProviderData),
            CreatedAt = DateTime.UtcNow
        };

        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment transaction {TransactionId} created for workspace {WorkspaceId}", 
            transaction.Id, request.WorkspaceId);

        return transaction;
    }

    public async Task UpdatePaymentTransactionAsync(string transactionId, PaymentResult result)
    {
        var transaction = await _context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.ProviderTransactionId == transactionId);

        if (transaction == null)
        {
            _logger.LogWarning("Payment transaction {TransactionId} not found for update", transactionId);
            return;
        }

        transaction.Status = result.Status;
        transaction.ProcessedAt = result.ProcessedAt ?? DateTime.UtcNow;
        transaction.ProviderResponse = System.Text.Json.JsonSerializer.Serialize(
            MergeProviderData(transaction.ProviderResponse, result.ProviderData));
        transaction.ErrorMessage = result.ErrorMessage;
        transaction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment transaction {TransactionId} updated with status {Status}", 
            transactionId, result.Status);
    }

    private void EnsureProviderDataDefaults(PaymentRequest request, PaymentResult result)
    {
        if (request.PlanType.HasValue && !result.ProviderData.ContainsKey("plan_type"))
        {
            result.ProviderData["plan_type"] = request.PlanType.Value.ToString();
        }

        if (!result.ProviderData.ContainsKey("workspace_id"))
        {
            result.ProviderData["workspace_id"] = request.WorkspaceId;
        }
    }

    private static Dictionary<string, object> MergeProviderData(string? existingJson, Dictionary<string, object> incoming)
    {
        var merged = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(existingJson))
        {
            try
            {
                var existing = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(existingJson);
                if (existing != null)
                {
                    foreach (var kvp in existing)
                    {
                        merged[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch
            {
                // Ignore JSON parse errors and just use incoming data
            }
        }

        foreach (var kvp in incoming)
        {
            merged[kvp.Key] = kvp.Value;
        }

        return merged;
    }

    public async Task<List<Invoice>> GetInvoicesAsync(int workspaceId)
    {
        return await _context.Invoices
            .Where(i => i.WorkspaceId == workspaceId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<Invoice> CreateInvoiceAsync(int workspaceId, PlanType planType, decimal amount, int? paymentTransactionId = null)
    {
        var invoice = new Invoice
        {
            WorkspaceId = workspaceId,
            InvoiceNumber = GenerateInvoiceNumber(),
            Amount = amount,
            Tax = amount * 0.20m, // %20 KDV
            Total = amount * 1.20m,
            DueDate = DateTime.UtcNow.AddDays(15),
            Status = InvoiceStatus.Pending,
            PlanType = planType,
            PeriodStart = DateTime.UtcNow,
            PeriodEnd = DateTime.UtcNow.AddMonths(1),
            PaymentTransactionId = paymentTransactionId,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem { Description = $"{planType} Plan", Amount = amount, Quantity = 1 }
            },
            CreatedAt = DateTime.UtcNow
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        return invoice;
    }

    private string GenerateInvoiceNumber()
    {
        var year = DateTime.UtcNow.Year;
        var sequence = DateTime.UtcNow.Ticks.ToString()[^6..]; // Son 6 hane
        return $"INV-{year}-{sequence}";
    }
}
