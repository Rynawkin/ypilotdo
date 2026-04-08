using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Services.Payment;
using Monolith.WebAPI.Services.Subscription;
using WorkspaceEntity = Monolith.WebAPI.Data.Workspace.Workspace;

namespace Monolith.WebAPI.Services.BackgroundJobs;

public sealed class SubscriptionRenewalJob : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionRenewalJob> _logger;

    public SubscriptionRenewalJob(IServiceScopeFactory scopeFactory, ILogger<SubscriptionRenewalJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = ActivatorUtilities.CreateInstance<SubscriptionRenewalProcessor>(scope.ServiceProvider);
                await processor.ProcessAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscription renewal cycle failed");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }
}

internal sealed class SubscriptionRenewalProcessor
{
    private static readonly TimeSpan GracePeriod = TimeSpan.FromDays(3);
    private static readonly TimeSpan RetryInterval = TimeSpan.FromHours(24);

    private readonly AppDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<SubscriptionRenewalProcessor> _logger;
    private readonly IConfiguration _configuration;

    public SubscriptionRenewalProcessor(
        AppDbContext context,
        IPaymentService paymentService,
        ISubscriptionService subscriptionService,
        ILogger<SubscriptionRenewalProcessor> logger,
        IConfiguration configuration)
    {
        _context = context;
        _paymentService = paymentService;
        _subscriptionService = subscriptionService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await BackfillMissingPlanDatesAsync(now, cancellationToken);
        await ProcessPendingGraceInvoicesAsync(now, cancellationToken);
        await ProcessDueRenewalsAsync(now, cancellationToken);
    }

    private async Task BackfillMissingPlanDatesAsync(DateTime now, CancellationToken cancellationToken)
    {
        var workspaces = await _context.Workspaces
            .Where(w => w.PlanType != PlanType.Trial && w.PlanEndDate == null)
            .ToListAsync(cancellationToken);

        foreach (var workspace in workspaces)
        {
            var latestPaidInvoice = await _context.Invoices
                .Where(i => i.WorkspaceId == workspace.Id && i.Status == InvoiceStatus.Paid)
                .OrderByDescending(i => i.PeriodEnd)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestPaidInvoice != null)
            {
                workspace.RenewPlanCycle(workspace.PlanType, latestPaidInvoice.PeriodStart);
                workspace.SetActive(true);
                workspace.SetPlanPeriod(latestPaidInvoice.PeriodStart, latestPaidInvoice.PeriodEnd);
            }
            else
            {
                workspace.UpdatePlan(workspace.PlanType);
            }
        }

        if (workspaces.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessPendingGraceInvoicesAsync(DateTime now, CancellationToken cancellationToken)
    {
        var invoices = await _context.Invoices
            .Where(i => (i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.Overdue) && i.PlanType != PlanType.Trial)
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);

        foreach (var invoice in invoices)
        {
            var workspace = await _context.Workspaces.FirstOrDefaultAsync(w => w.Id == invoice.WorkspaceId, cancellationToken);
            if (workspace == null)
            {
                continue;
            }

            if (invoice.DueDate <= now)
            {
                if (workspace.Active)
                {
                    workspace.SetActive(false);
                    invoice.Status = InvoiceStatus.Overdue;
                    _logger.LogWarning("Workspace {WorkspaceId} suspended after unpaid grace period ended", workspace.Id);
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                await AttemptRenewalAsync(workspace, invoice, now, cancellationToken);
            }
        }
    }

    private async Task ProcessDueRenewalsAsync(DateTime now, CancellationToken cancellationToken)
    {
        var workspaces = await _context.Workspaces
            .Where(w => w.PlanType != PlanType.Trial && w.PlanEndDate != null && w.PlanEndDate <= now)
            .OrderBy(w => w.PlanEndDate)
            .ToListAsync(cancellationToken);

        foreach (var workspace in workspaces)
        {
            var openInvoice = await _context.Invoices
                .Where(i => i.WorkspaceId == workspace.Id && (i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.Overdue))
                .OrderByDescending(i => i.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (openInvoice != null)
            {
                continue;
            }

            var invoice = await CreateRenewalInvoiceAsync(workspace, now, cancellationToken);
            await AttemptRenewalAsync(workspace, invoice, now, cancellationToken);
        }
    }

    private async Task<Invoice> CreateRenewalInvoiceAsync(WorkspaceEntity workspace, DateTime now, CancellationToken cancellationToken)
    {
        var limits = _subscriptionService.GetPlanLimits(workspace.PlanType);
        var amount = limits.MonthlyPrice + workspace.CurrentMonthAdditionalCharges;
        var invoice = await _paymentService.CreateInvoiceAsync(workspace.Id, workspace.PlanType, amount);
        invoice.DueDate = now.Add(GracePeriod);
        invoice.PeriodStart = workspace.PlanEndDate ?? now;
        invoice.PeriodEnd = invoice.PeriodStart.AddMonths(1);
        invoice.Status = InvoiceStatus.Pending;
        invoice.UpdatedAt = null;
        await _context.SaveChangesAsync(cancellationToken);
        return invoice;
    }

    private async Task AttemptRenewalAsync(WorkspaceEntity workspace, Invoice invoice, DateTime now, CancellationToken cancellationToken)
    {
        var shouldRetry = await ShouldRetryInvoiceAsync(invoice, now, cancellationToken);
        if (!shouldRetry)
        {
            return;
        }

        var paymentMethod = await _context.PaymentMethods
            .Where(pm => pm.WorkspaceId == workspace.Id && pm.IsActive && pm.IsDefault && pm.ProviderMethodId != null)
            .OrderByDescending(pm => pm.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (paymentMethod == null)
        {
            _logger.LogWarning("Workspace {WorkspaceId} has no active default payment method for renewal", workspace.Id);
            invoice.Status = invoice.DueDate <= now ? InvoiceStatus.Overdue : InvoiceStatus.Pending;
            invoice.UpdatedAt = now;
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        var adminEmail = await _context.Users
            .Where(u => u.WorkspaceId == workspace.Id && u.IsAdmin)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken) ?? workspace.Email ?? string.Empty;

        var chargeRequest = new StoredPaymentChargeRequest
        {
            WorkspaceId = workspace.Id,
            ProviderMethodId = paymentMethod.ProviderMethodId!,
            Amount = invoice.Amount,
            Currency = "TRY",
            Description = $"{workspace.PlanType} plan monthly renewal",
            CustomerEmail = adminEmail,
            CustomerName = workspace.Name,
            CustomerPhone = workspace.PhoneNumber ?? string.Empty,
            ClientIp = "127.0.0.1",
            ReferrerUrl = _configuration["Tracking:BaseUrl"] ?? _configuration["AppUrl"] ?? "https://app.yolpilot.com",
            PlanType = workspace.PlanType,
            ExtraData = new Dictionary<string, object>
            {
                ["renewal_mode"] = true,
                ["invoice_id"] = invoice.Id,
                ["provider"] = paymentMethod.Provider ?? string.Empty,
                ["payment_method_provider_id"] = paymentMethod.ProviderMethodId!
            }
        };

        var result = await _paymentService.ChargeStoredPaymentMethodAsync(chargeRequest);
        if (result.InternalTransactionId.HasValue)
        {
            invoice.PaymentTransactionId = result.InternalTransactionId.Value;
        }

        if (!result.IsSuccess)
        {
            invoice.Status = invoice.DueDate <= now ? InvoiceStatus.Overdue : InvoiceStatus.Pending;
            invoice.UpdatedAt = now;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Workspace {WorkspaceId} renewal attempt failed: {Error}", workspace.Id, result.ErrorMessage);
            return;
        }

        workspace.RenewPlanCycle(workspace.PlanType, now);
        workspace.SyncLegacyDriverLimit(_subscriptionService.GetPlanLimits(workspace.PlanType).MaxDrivers);
        workspace.SetActive(true);
        workspace.ResetMonthlyUsage();

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidDate = now;
        invoice.DueDate = now;
        invoice.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Workspace {WorkspaceId} renewed successfully until {PlanEndDate}", workspace.Id, workspace.PlanEndDate);
    }

    private async Task<bool> ShouldRetryInvoiceAsync(Invoice invoice, DateTime now, CancellationToken cancellationToken)
    {
        if (!invoice.PaymentTransactionId.HasValue)
        {
            return !invoice.UpdatedAt.HasValue || invoice.UpdatedAt.Value <= now.Subtract(RetryInterval);
        }

        var transaction = await _context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.Id == invoice.PaymentTransactionId.Value, cancellationToken);

        if (transaction == null)
        {
            return true;
        }

        return transaction.CreatedAt <= now.Subtract(RetryInterval);
    }
}
