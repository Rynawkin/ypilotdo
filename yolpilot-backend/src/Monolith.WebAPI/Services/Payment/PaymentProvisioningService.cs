using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands.Workspace;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Data.Seed;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Services.Subscription;

namespace Monolith.WebAPI.Services.Payment;

public interface IPaymentProvisioningService
{
    Task EnsureProvisionedAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);
}

public class PaymentProvisioningService : IPaymentProvisioningService
{
    private readonly AppDbContext _context;
    private readonly ISubscriptionService _subscriptionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentProvisioningService> _logger;

    public PaymentProvisioningService(
        AppDbContext context,
        ISubscriptionService subscriptionService,
        UserManager<ApplicationUser> userManager,
        IPaymentService paymentService,
        ILogger<PaymentProvisioningService> logger)
    {
        _context = context;
        _subscriptionService = subscriptionService;
        _userManager = userManager;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task EnsureProvisionedAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (transaction.Status != PaymentStatus.Completed)
            return;

        var providerData = DeserializeProviderData(transaction.ProviderResponse);
        if (providerData == null)
            return;

        if (!TryGetPlanType(providerData, out var planType))
            return;

        if (GetBool(providerData, "signup_mode"))
        {
            await CompleteSignupAsync(transaction, providerData, planType, cancellationToken);
            return;
        }

        await CompletePlanUpgradeAsync(transaction, providerData, planType, cancellationToken);
    }

    private async Task CompletePlanUpgradeAsync(
        PaymentTransaction transaction,
        Dictionary<string, object> providerData,
        PlanType planType,
        CancellationToken cancellationToken)
    {
        var workspace = transaction.Workspace ?? await _context.Workspaces.FirstOrDefaultAsync(w => w.Id == transaction.WorkspaceId, cancellationToken);
        if (workspace == null)
        {
            _logger.LogWarning("Workspace not found for payment transaction {TransactionId}", transaction.Id);
            return;
        }

        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.PaymentTransactionId == transaction.Id, cancellationToken);
        if (invoice?.Status == InvoiceStatus.Paid && workspace.PlanType == planType)
            return;

        var currentPlan = workspace.PlanType;
        var isDowngrade = IsDowngrade(currentPlan, planType);

        if (isDowngrade)
        {
            var newPlanLimits = _subscriptionService.GetPlanLimits(planType);

            var activeDriversCount = await _context.Users.CountAsync(u => u.WorkspaceId == transaction.WorkspaceId && u.IsDriver, cancellationToken);
            if (newPlanLimits.MaxDrivers.HasValue && activeDriversCount > newPlanLimits.MaxDrivers.Value)
            {
                await BlockDowngradeAsync(transaction, invoice,
                    $"Plan downgrade blocked: {activeDriversCount} active drivers exceeds {planType} limit of {newPlanLimits.MaxDrivers.Value}",
                    cancellationToken);
                return;
            }

            var activeVehiclesCount = await _context.Vehicles.CountAsync(v => v.WorkspaceId == transaction.WorkspaceId && !v.IsDeleted, cancellationToken);
            if (newPlanLimits.MaxVehicles.HasValue && activeVehiclesCount > newPlanLimits.MaxVehicles.Value)
            {
                await BlockDowngradeAsync(transaction, invoice,
                    $"Plan downgrade blocked: {activeVehiclesCount} active vehicles exceeds {planType} limit of {newPlanLimits.MaxVehicles.Value}",
                    cancellationToken);
                return;
            }

            var activeCustomersCount = await _context.Customers.CountAsync(c => c.WorkspaceId == transaction.WorkspaceId && !c.IsDeleted, cancellationToken);
            if (newPlanLimits.MaxCustomers.HasValue && activeCustomersCount > newPlanLimits.MaxCustomers.Value)
            {
                await BlockDowngradeAsync(transaction, invoice,
                    $"Plan downgrade blocked: {activeCustomersCount} active customers exceeds {planType} limit of {newPlanLimits.MaxCustomers.Value}",
                    cancellationToken);
                return;
            }

            var activeUsersCount = await _context.Users.CountAsync(u => u.WorkspaceId == transaction.WorkspaceId, cancellationToken);
            if (newPlanLimits.MaxUsers.HasValue && activeUsersCount > newPlanLimits.MaxUsers.Value)
            {
                await BlockDowngradeAsync(transaction, invoice,
                    $"Plan downgrade blocked: {activeUsersCount} active users exceeds {planType} limit of {newPlanLimits.MaxUsers.Value}",
                    cancellationToken);
                return;
            }
        }

        workspace.UpdatePlan(planType);
        workspace.SyncLegacyDriverLimit(_subscriptionService.GetPlanLimits(planType).MaxDrivers);
        workspace.SetActive(true);

        if (invoice == null)
        {
            invoice = await _paymentService.CreateInvoiceAsync(
                workspace.Id,
                planType,
                _subscriptionService.GetPlanLimits(planType).MonthlyPrice,
                transaction.Id);
        }

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task CompleteSignupAsync(
        PaymentTransaction transaction,
        Dictionary<string, object> providerData,
        PlanType planType,
        CancellationToken cancellationToken)
    {
        var workspace = transaction.Workspace ?? await _context.Workspaces.FirstOrDefaultAsync(w => w.Id == transaction.WorkspaceId, cancellationToken);
        if (workspace == null)
        {
            _logger.LogWarning("Pending signup workspace not found for transaction {TransactionId}", transaction.Id);
            return;
        }

        var adminEmail = GetString(providerData, "admin_email");
        var adminFullName = GetString(providerData, "admin_full_name");
        var passwordHash = GetString(providerData, "admin_password_hash");
        var companyName = GetString(providerData, "company_name");
        var companyEmail = GetString(providerData, "company_email");
        var companyPhone = GetString(providerData, "company_phone");

        if (string.IsNullOrWhiteSpace(adminEmail) ||
            string.IsNullOrWhiteSpace(adminFullName) ||
            string.IsNullOrWhiteSpace(passwordHash))
        {
            transaction.Status = PaymentStatus.Failed;
            transaction.ErrorMessage = "Signup metadata is incomplete";
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail, cancellationToken);
        if (user == null)
        {
            var createWorkspaceCommand = new CreateWorkspaceCommand
            {
                WorkspaceName = companyName ?? workspace.Name,
                AdminUserEmail = adminEmail,
                AdminUserFullName = adminFullName,
                AdminUserPassword = "paid-signup"
            };

            user = new ApplicationUser(createWorkspaceCommand, workspace)
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                PasswordHash = passwordHash
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                transaction.Status = PaymentStatus.Failed;
                transaction.ErrorMessage = string.Join(", ", createResult.Errors.Select(e => e.Description));
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            await _userManager.AddToRoleAsync(user, "Admin");

            var hasTemplates = await _context.MessageTemplates.AnyAsync(t => t.WorkspaceId == workspace.Id, cancellationToken);
            if (!hasTemplates)
            {
                var defaultTemplates = DefaultTemplates.GetDefaultTemplates(workspace.Id);
                _context.MessageTemplates.AddRange(defaultTemplates);
            }
        }

        if (!string.IsNullOrWhiteSpace(companyName))
            workspace.Name = companyName;
        if (!string.IsNullOrWhiteSpace(companyEmail))
            workspace.Email = companyEmail;
        if (!string.IsNullOrWhiteSpace(companyPhone))
            workspace.PhoneNumber = companyPhone;

        workspace.UpdatePlan(planType);
        workspace.SyncLegacyDriverLimit(_subscriptionService.GetPlanLimits(planType).MaxDrivers);
        workspace.SetActive(true);

        transaction.WorkspaceId = workspace.Id;

        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.PaymentTransactionId == transaction.Id, cancellationToken);
        if (invoice == null)
        {
            invoice = await _paymentService.CreateInvoiceAsync(
                workspace.Id,
                planType,
                _subscriptionService.GetPlanLimits(planType).MonthlyPrice,
                transaction.Id);
        }

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task BlockDowngradeAsync(
        PaymentTransaction transaction,
        Invoice? invoice,
        string message,
        CancellationToken cancellationToken)
    {
        transaction.Status = PaymentStatus.Failed;
        transaction.ErrorMessage = message;
        transaction.ProviderResponse = message;
        if (invoice != null)
        {
            invoice.Status = InvoiceStatus.Cancelled;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static Dictionary<string, object>? DeserializeProviderData(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryGetPlanType(Dictionary<string, object> data, out PlanType planType)
    {
        var raw = GetString(data, "plan_type");
        return Enum.TryParse(raw, true, out planType);
    }

    private static bool GetBool(Dictionary<string, object> data, string key)
    {
        var raw = GetString(data, key);
        return bool.TryParse(raw, out var value) && value;
    }

    private static string? GetString(Dictionary<string, object> data, string key)
    {
        if (!data.TryGetValue(key, out var value) || value == null)
            return null;

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => element.ToString()
            };
        }

        return value.ToString();
    }

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
}
