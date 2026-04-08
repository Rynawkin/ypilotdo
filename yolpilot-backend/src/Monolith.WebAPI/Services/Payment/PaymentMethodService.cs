using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Services.Payment;

public interface IPaymentMethodService
{
    Task<PaymentMethodEntity?> GetDefaultAsync(int workspaceId, CancellationToken cancellationToken = default);
    Task<PaymentMethodEntity?> UpsertFromProviderDataAsync(int workspaceId, Dictionary<string, object> providerData, CancellationToken cancellationToken = default);
}

public class PaymentMethodService : IPaymentMethodService
{
    private readonly AppDbContext _context;

    public PaymentMethodService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentMethodEntity?> GetDefaultAsync(int workspaceId, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentMethods
            .AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId && x.IsActive && x.IsDefault)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PaymentMethodEntity?> UpsertFromProviderDataAsync(int workspaceId, Dictionary<string, object> providerData, CancellationToken cancellationToken = default)
    {
        var providerMethodId = GetString(providerData, "payment_method_provider_id");
        if (string.IsNullOrWhiteSpace(providerMethodId))
        {
            return null;
        }

        var provider = GetString(providerData, "provider_name") ?? GetString(providerData, "provider") ?? "ParamPOS";
        var existing = await _context.PaymentMethods
            .FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.ProviderMethodId == providerMethodId, cancellationToken);

        var currentDefaults = await _context.PaymentMethods
            .Where(x => x.WorkspaceId == workspaceId && x.IsDefault && (existing == null || x.Id != existing.Id))
            .ToListAsync(cancellationToken);

        foreach (var currentDefault in currentDefaults)
        {
            currentDefault.IsDefault = false;
            currentDefault.UpdatedAt = DateTime.UtcNow;
        }

        if (existing == null)
        {
            existing = new PaymentMethodEntity
            {
                WorkspaceId = workspaceId,
                Type = PaymentMethodType.CreditCard,
                IsDefault = true,
                Provider = provider,
                ProviderMethodId = providerMethodId,
                LastFourDigits = GetString(providerData, "payment_method_last4"),
                CardHolderName = GetString(providerData, "payment_method_holder"),
                ExpiryMonth = GetString(providerData, "payment_method_expiry_month"),
                ExpiryYear = GetString(providerData, "payment_method_expiry_year"),
                BrandName = GetString(providerData, "payment_method_brand"),
                IsActive = true
            };

            _context.PaymentMethods.Add(existing);
        }
        else
        {
            existing.IsActive = true;
            existing.IsDefault = true;
            existing.Provider = provider;
            existing.LastFourDigits = GetString(providerData, "payment_method_last4") ?? existing.LastFourDigits;
            existing.CardHolderName = GetString(providerData, "payment_method_holder") ?? existing.CardHolderName;
            existing.ExpiryMonth = GetString(providerData, "payment_method_expiry_month") ?? existing.ExpiryMonth;
            existing.ExpiryYear = GetString(providerData, "payment_method_expiry_year") ?? existing.ExpiryYear;
            existing.BrandName = GetString(providerData, "payment_method_brand") ?? existing.BrandName;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    private static string? GetString(Dictionary<string, object> data, string key)
    {
        if (!data.TryGetValue(key, out var value) || value == null)
        {
            return null;
        }

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
}
