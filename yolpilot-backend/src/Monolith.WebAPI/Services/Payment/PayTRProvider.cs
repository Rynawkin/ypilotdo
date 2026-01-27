using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Services.Payment;

public class PayTRProvider : IPaymentProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PayTRProvider> _logger;
    private readonly HttpClient _httpClient;

    public PayTRProvider(IConfiguration configuration, ILogger<PayTRProvider> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public string ProviderName => "PayTR";

    public async Task<PaymentResult> InitiatePaymentAsync(PaymentRequest request)
    {
        try
        {
            var merchantId = _configuration["Payment:PayTR:MerchantId"];
            var merchantKey = _configuration["Payment:PayTR:MerchantKey"];
            var merchantSalt = _configuration["Payment:PayTR:MerchantSalt"];
            var testMode = _configuration.GetValue<bool>("Payment:PayTR:TestMode");

            if (string.IsNullOrEmpty(merchantId) || string.IsNullOrEmpty(merchantKey) || string.IsNullOrEmpty(merchantSalt))
            {
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "PayTR configuration is missing"
                };
            }

            // PayTR için gerekli parametreler
            var merchantOid = Guid.NewGuid().ToString("N")[..16]; // Unique order ID
            var userBasket = JsonSerializer.Serialize(new[]
            {
                new { name = request.Description, price = request.Amount.ToString("F2"), quantity = 1 }
            });

            var paytrToken = GeneratePayTRToken(merchantId, merchantKey, merchantSalt, merchantOid, 
                request.Amount, request.Currency, userBasket, request.CustomerEmail);

            var formData = new Dictionary<string, string>
            {
                ["merchant_id"] = merchantId,
                ["user_ip"] = "127.0.0.1", // Bu gerçek IP'den alınmalı
                ["merchant_oid"] = merchantOid,
                ["email"] = request.CustomerEmail,
                ["payment_amount"] = ((int)(request.Amount * 100)).ToString(), // Kuruş cinsinden
                ["paytr_token"] = paytrToken,
                ["user_basket"] = userBasket,
                ["debug_on"] = testMode ? "1" : "0",
                ["no_installment"] = "0",
                ["max_installment"] = "0",
                ["user_name"] = request.CustomerName,
                ["user_address"] = "N/A",
                ["user_phone"] = request.CustomerPhone,
                ["merchant_ok_url"] = request.SuccessUrl ?? "",
                ["merchant_fail_url"] = request.FailUrl ?? "",
                ["timeout_limit"] = "30",
                ["currency"] = request.Currency,
                ["test_mode"] = testMode ? "1" : "0"
            };

            // PayTR API'ye form post
            var formContent = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync("https://www.paytr.com/odeme/api/get-token", formContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            var paytrResponse = JsonSerializer.Deserialize<PayTRTokenResponse>(responseContent);

            if (paytrResponse?.status == "success")
            {
                return new PaymentResult
                {
                    IsSuccess = true,
                    TransactionId = merchantOid,
                    PaymentUrl = $"https://www.paytr.com/odeme/guvenli/{paytrResponse.token}",
                    Status = PaymentStatus.Pending,
                    ProviderData = new Dictionary<string, object>
                    {
                        ["paytr_token"] = paytrResponse.token,
                        ["merchant_oid"] = merchantOid
                    }
                };
            }

            return new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = paytrResponse?.reason ?? "PayTR payment initiation failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayTR payment initiation failed for workspace {WorkspaceId}", request.WorkspaceId);
            return new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = "Payment initiation failed"
            };
        }
    }

    public async Task<PaymentResult> ProcessWebhookAsync(string payload, Dictionary<string, string> headers)
    {
        try
        {
            // PayTR webhook işleme placeholder
            _logger.LogInformation("Processing PayTR webhook: {Payload}", payload);
            
            // TODO: PayTR webhook signature doğrulaması ve payload parsing
            
            return new PaymentResult
            {
                IsSuccess = true,
                Status = PaymentStatus.Completed,
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayTR webhook processing failed");
            return new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = "Webhook processing failed"
            };
        }
    }

    public async Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount)
    {
        // PayTR refund implementation placeholder
        _logger.LogWarning("PayTR refund not implemented for transaction {TransactionId}", transactionId);
        
        return new PaymentResult
        {
            IsSuccess = false,
            ErrorMessage = "Refund not implemented for PayTR"
        };
    }

    public async Task<PaymentResult> GetPaymentStatusAsync(string transactionId)
    {
        // PayTR status check implementation placeholder
        _logger.LogInformation("Checking PayTR payment status for {TransactionId}", transactionId);
        
        return new PaymentResult
        {
            IsSuccess = true,
            Status = PaymentStatus.Pending,
            TransactionId = transactionId
        };
    }

    private string GeneratePayTRToken(string merchantId, string merchantKey, string merchantSalt, 
        string merchantOid, decimal amount, string currency, string userBasket, string email)
    {
        var amountStr = ((int)(amount * 100)).ToString(); // Kuruş cinsinden
        var hashStr = $"{merchantId}{merchantOid}{amountStr}{merchantSalt}";
        
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(merchantKey)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(hashStr));
            return Convert.ToBase64String(hash);
        }
    }
}

internal class PayTRTokenResponse
{
    public string? status { get; set; }
    public string? token { get; set; }
    public string? reason { get; set; }
}