using System.Text;
using System.Text.Json;
using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Services.Payment;

public class ParamPOSProvider : IPaymentProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ParamPOSProvider> _logger;
    private readonly HttpClient _httpClient;

    public ParamPOSProvider(IConfiguration configuration, ILogger<ParamPOSProvider> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public string ProviderName => "ParamPOS";

    public async Task<PaymentResult> InitiatePaymentAsync(PaymentRequest request)
    {
        try
        {
            var clientCode = _configuration["Payment:ParamPOS:ClientCode"];
            var clientUsername = _configuration["Payment:ParamPOS:ClientUsername"];
            var clientPassword = _configuration["Payment:ParamPOS:ClientPassword"];
            var testMode = _configuration.GetValue<bool>("Payment:ParamPOS:TestMode");

            if (string.IsNullOrEmpty(clientCode) || string.IsNullOrEmpty(clientUsername) || string.IsNullOrEmpty(clientPassword))
            {
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "ParamPOS configuration is missing"
                };
            }

            var orderId = Guid.NewGuid().ToString("N")[..16];
            
            var paramRequest = new
            {
                CLIENT_CODE = clientCode,
                CLIENT_USERNAME = clientUsername,
                CLIENT_PASSWORD = clientPassword,
                AMOUNT = request.Amount.ToString("F2"),
                CURRENCY = request.Currency,
                ORDER_ID = orderId,
                CUSTOMER_EMAIL = request.CustomerEmail,
                CUSTOMER_NAME = request.CustomerName,
                CUSTOMER_PHONE = request.CustomerPhone,
                SUCCESS_URL = request.SuccessUrl,
                FAIL_URL = request.FailUrl,
                TEST_MODE = testMode ? "1" : "0"
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(paramRequest), Encoding.UTF8, "application/json");
            
            var baseUrl = testMode ? "https://test.param.com.tr" : "https://api.param.com.tr";
            var response = await _httpClient.PostAsync($"{baseUrl}/payment/create", jsonContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            var paramResponse = JsonSerializer.Deserialize<ParamPOSResponse>(responseContent);

            if (paramResponse?.IsSuccess == true)
            {
                return new PaymentResult
                {
                    IsSuccess = true,
                    TransactionId = orderId,
                    PaymentUrl = paramResponse.PaymentUrl,
                    Status = PaymentStatus.Pending,
                    ProviderData = new Dictionary<string, object>
                    {
                        ["order_id"] = orderId,
                        ["param_response"] = responseContent
                    }
                };
            }

            return new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = paramResponse?.ErrorMessage ?? "ParamPOS payment initiation failed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ParamPOS payment initiation failed for workspace {WorkspaceId}", request.WorkspaceId);
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
            _logger.LogInformation("Processing ParamPOS webhook: {Payload}", payload);
            
            // TODO: ParamPOS webhook signature doğrulaması ve payload parsing
            
            return new PaymentResult
            {
                IsSuccess = true,
                Status = PaymentStatus.Completed,
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ParamPOS webhook processing failed");
            return new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = "Webhook processing failed"
            };
        }
    }

    public async Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount)
    {
        _logger.LogWarning("ParamPOS refund not implemented for transaction {TransactionId}", transactionId);
        
        return new PaymentResult
        {
            IsSuccess = false,
            ErrorMessage = "Refund not implemented for ParamPOS"
        };
    }

    public async Task<PaymentResult> GetPaymentStatusAsync(string transactionId)
    {
        _logger.LogInformation("Checking ParamPOS payment status for {TransactionId}", transactionId);
        
        return new PaymentResult
        {
            IsSuccess = true,
            Status = PaymentStatus.Pending,
            TransactionId = transactionId
        };
    }
}

internal class ParamPOSResponse
{
    public bool IsSuccess { get; set; }
    public string? PaymentUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TransactionId { get; set; }
}