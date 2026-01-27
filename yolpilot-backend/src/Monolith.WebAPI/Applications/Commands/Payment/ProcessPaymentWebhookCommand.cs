using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Services.Payment;

namespace Monolith.WebAPI.Applications.Commands.Payment;

public class ProcessPaymentWebhookCommand : IRequest<ProcessPaymentWebhookResponse>
{
    public string Provider { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
}

public class ProcessPaymentWebhookResponse
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TransactionId { get; set; }
    public PaymentStatus Status { get; set; }
}

public class ProcessPaymentWebhookCommandHandler : IRequestHandler<ProcessPaymentWebhookCommand, ProcessPaymentWebhookResponse>
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<ProcessPaymentWebhookCommandHandler> _logger;
    private readonly AppDbContext _context;

    public ProcessPaymentWebhookCommandHandler(
        IPaymentService paymentService,
        ILogger<ProcessPaymentWebhookCommandHandler> logger,
        AppDbContext context)
    {
        _paymentService = paymentService;
        _logger = logger;
        _context = context;
    }

    public async Task<ProcessPaymentWebhookResponse> Handle(ProcessPaymentWebhookCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing {Provider} webhook", request.Provider);

            var result = await _paymentService.ProcessWebhookAsync(request.Provider, request.Payload, request.Headers);

            if (result.IsSuccess && result.Status == PaymentStatus.Completed && !string.IsNullOrEmpty(result.TransactionId))
            {
                // Ödeme başarılı ise workspace planını güncelle
                await UpdateWorkspacePlanAsync(result.TransactionId);
            }

            return new ProcessPaymentWebhookResponse
            {
                IsSuccess = result.IsSuccess,
                TransactionId = result.TransactionId,
                Status = result.Status,
                ErrorMessage = result.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {Provider} webhook", request.Provider);
            return new ProcessPaymentWebhookResponse
            {
                IsSuccess = false,
                ErrorMessage = "Webhook processing failed"
            };
        }
    }

    private async Task UpdateWorkspacePlanAsync(string transactionId)
    {
        var transaction = await _context.PaymentTransactions
            .Include(t => t.Workspace)
            .FirstOrDefaultAsync(t => t.ProviderTransactionId == transactionId);

        if (transaction?.Workspace == null)
        {
            _logger.LogWarning("Transaction {TransactionId} or workspace not found for plan update", transactionId);
            return;
        }

        // SECURITY: Idempotency check - prevent double processing of payment webhooks
        if (transaction.Status == PaymentStatus.Completed)
        {
            _logger.LogInformation("Transaction {TransactionId} already processed, skipping duplicate webhook", transactionId);
            return;
        }

        // Mark transaction as completed to prevent double processing
        transaction.Status = PaymentStatus.Completed;

        // Provider response'tan plan tipini çıkar
        try
        {
            var providerData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                transaction.ProviderResponse ?? "{}");

            if (providerData?.ContainsKey("plan_type") == true &&
                Enum.TryParse<PlanType>(providerData["plan_type"].ToString(), out var planType))
            {
                // BUGFIX S3.14: Check current usage before downgrading plan
                var currentPlan = transaction.Workspace.PlanType;
                var isDowngrade = IsDowngrade(currentPlan, planType);

                if (isDowngrade)
                {
                    _logger.LogWarning("BUGFIX S3.14: Plan downgrade detected: {CurrentPlan} -> {NewPlan}. Checking usage...",
                        currentPlan, planType);

                    // Check active drivers count
                    var activeDriversCount = await _context.Users
                        .CountAsync(u => u.WorkspaceId == transaction.WorkspaceId &&
                                        u.IsDriver);

                    var newPlanLimits = GetPlanLimits(planType);

                    if (activeDriversCount > newPlanLimits.MaxDrivers)
                    {
                        _logger.LogError(
                            "BUGFIX S3.14: Cannot downgrade to {NewPlan}. Active drivers ({ActiveCount}) exceeds limit ({Limit})",
                            planType, activeDriversCount, newPlanLimits.MaxDrivers);

                        // Mark transaction as failed
                        transaction.Status = PaymentStatus.Failed;
                        transaction.ProviderResponse = $"Plan downgrade blocked: {activeDriversCount} active drivers exceeds {planType} limit of {newPlanLimits.MaxDrivers}";
                        await _context.SaveChangesAsync();
                        return;
                    }

                    // Check active vehicles count
                    var activeVehiclesCount = await _context.Vehicles
                        .CountAsync(v => v.WorkspaceId == transaction.WorkspaceId && !v.IsDeleted);

                    if (activeVehiclesCount > newPlanLimits.MaxVehicles)
                    {
                        _logger.LogError(
                            "BUGFIX S3.14: Cannot downgrade to {NewPlan}. Active vehicles ({ActiveCount}) exceeds limit ({Limit})",
                            planType, activeVehiclesCount, newPlanLimits.MaxVehicles);

                        transaction.Status = PaymentStatus.Failed;
                        transaction.ProviderResponse = $"Plan downgrade blocked: {activeVehiclesCount} active vehicles exceeds {planType} limit of {newPlanLimits.MaxVehicles}";
                        await _context.SaveChangesAsync();
                        return;
                    }

                    _logger.LogInformation("BUGFIX S3.14: Usage check passed. Downgrade allowed.");
                }

                // Workspace planını güncelle
                transaction.Workspace.UpdatePlan(planType);

                // Trial'dan çıkıyorsa trial bitiş tarihini güncelle
                if (transaction.Workspace.PlanType == PlanType.Trial)
                {
                    transaction.Workspace.TrialEndDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Workspace {WorkspaceId} plan updated to {PlanType} after successful payment {TransactionId}",
                    transaction.WorkspaceId, planType, transactionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workspace plan for transaction {TransactionId}", transactionId);
        }
    }

    // BUGFIX S3.14: Helper methods for plan downgrade validation
    private static bool IsDowngrade(PlanType current, PlanType target)
    {
        var planOrder = new Dictionary<PlanType, int>
        {
            { PlanType.Trial, 0 },
            { PlanType.Starter, 1 },
            { PlanType.Growth, 2 },
            { PlanType.Professional, 3 },
            { PlanType.Business, 4 }
        };

        return planOrder.TryGetValue(current, out var currentOrder) &&
               planOrder.TryGetValue(target, out var targetOrder) &&
               targetOrder < currentOrder;
    }

    private static (int MaxDrivers, int MaxVehicles) GetPlanLimits(PlanType planType)
    {
        return planType switch
        {
            PlanType.Trial => (2, 2),
            PlanType.Starter => (5, 5),
            PlanType.Growth => (10, 10),
            PlanType.Professional => (20, 20),
            PlanType.Business => (int.MaxValue, int.MaxValue),
            _ => (0, 0)
        };
    }
}