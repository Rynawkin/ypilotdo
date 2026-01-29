using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System.Linq;
using Microsoft.AspNetCore.WebUtilities;
using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Services.Payment;

public class ParamPOSProvider : IPaymentProvider
{
    private const string DefaultXmlNamespace = "http://tempuri.org/";
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
            var settings = GetSettings();
            if (!settings.IsValid)
            {
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "ParamPOS configuration is missing"
                };
            }

            if (request.Card == null ||
                string.IsNullOrWhiteSpace(request.Card.CardNumber) ||
                string.IsNullOrWhiteSpace(request.Card.Cvv) ||
                string.IsNullOrWhiteSpace(request.Card.ExpiryMonth) ||
                string.IsNullOrWhiteSpace(request.Card.ExpiryYear))
            {
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Card details are required"
                };
            }

            var orderId = GenerateOrderId(request.WorkspaceId);
            var amountStr = FormatAmount(request.Amount);
            var totalStr = amountStr;
            var installment = "1";
            var islemHash = ComputeSha256Base64($"{settings.ClientCode}{settings.Guid}{installment}{amountStr}{totalStr}{orderId}");

            var successReturnUrl = BuildReturnUrl(settings.ApiBaseUrl, "success");
            var failReturnUrl = BuildReturnUrl(settings.ApiBaseUrl, "fail");

            var cardHolderName = string.IsNullOrWhiteSpace(request.Card.CardHolderName)
                ? request.CustomerName
                : request.Card.CardHolderName;
            var cardNumber = NormalizeCardNumber(request.Card.CardNumber);
            var cardMonth = NormalizeMonth(request.Card.ExpiryMonth);
            var cardYear = NormalizeYear(request.Card.ExpiryYear);
            var cardHolderPhone = NormalizePhone(request.Card.CardHolderPhone ?? request.CustomerPhone);
            var clientIp = request.ClientIp ?? settings.FallbackClientIp;

            if (string.IsNullOrWhiteSpace(cardHolderPhone) || cardHolderPhone.Length < 10)
            {
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Card holder phone is required"
                };
            }

            var body = new XElement(settings.XmlNamespace + "TP_WMD_UCD",
                BuildSecurityNode(settings),
                new XElement(settings.XmlNamespace + "GUID", settings.Guid),
                new XElement(settings.XmlNamespace + "KK_Sahibi", cardHolderName),
                new XElement(settings.XmlNamespace + "KK_No", cardNumber),
                new XElement(settings.XmlNamespace + "KK_SK_Ay", cardMonth),
                new XElement(settings.XmlNamespace + "KK_SK_Yil", cardYear),
                new XElement(settings.XmlNamespace + "KK_CVC", request.Card.Cvv),
                new XElement(settings.XmlNamespace + "KK_Sahibi_GSM", cardHolderPhone),
                new XElement(settings.XmlNamespace + "Hata_URL", failReturnUrl),
                new XElement(settings.XmlNamespace + "Basarili_URL", successReturnUrl),
                new XElement(settings.XmlNamespace + "Siparis_ID", orderId),
                new XElement(settings.XmlNamespace + "Siparis_Aciklama", request.Description),
                new XElement(settings.XmlNamespace + "Taksit", installment),
                new XElement(settings.XmlNamespace + "Islem_Tutar", amountStr),
                new XElement(settings.XmlNamespace + "Toplam_Tutar", totalStr),
                new XElement(settings.XmlNamespace + "Islem_Hash", islemHash),
                new XElement(settings.XmlNamespace + "Islem_Guvenlik_Tip", "3D"),
                new XElement(settings.XmlNamespace + "Islem_ID", string.Empty),
                new XElement(settings.XmlNamespace + "IPAdr", clientIp),
                new XElement(settings.XmlNamespace + "Ref_URL", request.ReferrerUrl ?? request.SuccessUrl ?? settings.AppBaseUrl),
                new XElement(settings.XmlNamespace + "Data1", request.WorkspaceId.ToString(CultureInfo.InvariantCulture)),
                new XElement(settings.XmlNamespace + "Data2", request.PlanType?.ToString() ?? string.Empty),
                new XElement(settings.XmlNamespace + "Data3", request.CustomerEmail),
                new XElement(settings.XmlNamespace + "Data4", request.CustomerPhone),
                new XElement(settings.XmlNamespace + "Data5", request.ExtraData.TryGetValue("upgrade_from", out var upgradeFrom) ? upgradeFrom?.ToString() : string.Empty)
            );

            var responseDoc = await SendSoapRequestAsync(settings, "TP_WMD_UCD", body);

            var sonuc = GetInt(responseDoc, "Sonuc");
            var sonucStr = GetString(responseDoc, "Sonuc_Str");
            var islemGuid = GetString(responseDoc, "Islem_GUID");
            var ucdHtml = GetString(responseDoc, "UCD_HTML");
            var ucdMd = GetString(responseDoc, "UCD_MD");
            var islemId = GetString(responseDoc, "Islem_ID");
            var bankTransId = GetString(responseDoc, "Bank_Trans_ID");
            var bankAuthCode = GetString(responseDoc, "Bank_AuthCode");

            if (sonuc <= 0)
            {
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = sonucStr ?? "ParamPOS payment initiation failed"
                };
            }

            var providerData = new Dictionary<string, object>
            {
                ["order_id"] = orderId,
                ["islem_guid"] = islemGuid ?? string.Empty,
                ["ucd_md"] = ucdMd ?? string.Empty,
                ["ucd_html"] = ucdHtml ?? string.Empty,
                ["islem_id"] = islemId ?? string.Empty,
                ["bank_trans_id"] = bankTransId ?? string.Empty,
                ["bank_auth_code"] = bankAuthCode ?? string.Empty,
                ["success_url"] = request.SuccessUrl ?? string.Empty,
                ["fail_url"] = request.FailUrl ?? string.Empty
            };

            if (string.Equals(ucdHtml, "NONSECURE", StringComparison.OrdinalIgnoreCase))
            {
                return new PaymentResult
                {
                    IsSuccess = true,
                    TransactionId = orderId,
                    Status = PaymentStatus.Completed,
                    ProcessedAt = DateTime.UtcNow,
                    ProviderData = providerData
                };
            }

            return new PaymentResult
            {
                IsSuccess = true,
                TransactionId = orderId,
                PaymentUrl = $"{settings.ApiBaseUrl.TrimEnd('/')}/api/payment/parampos/3d/{orderId}",
                Status = PaymentStatus.Pending,
                ProviderData = providerData
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
            var settings = GetSettings();
            if (!settings.IsValid)
            {
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "ParamPOS configuration is missing"
                };
            }

            var form = ParsePayload(payload);

            if (form.TryGetValue("mdStatus", out var mdStatusValue) || form.ContainsKey("md"))
            {
                return await HandleThreeDSecureReturnAsync(settings, form);
            }

            if (form.TryGetValue("TURKPOS_RETVAL_Sonuc", out _))
            {
                return HandleNonSecureReturn(settings, form);
            }

            return new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = "Unknown ParamPOS callback format"
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

    public Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount)
    {
        _logger.LogWarning("ParamPOS refund not implemented for transaction {TransactionId}", transactionId);

        return Task.FromResult(new PaymentResult
        {
            IsSuccess = false,
            ErrorMessage = "Refund not implemented for ParamPOS"
        });
    }

    public Task<PaymentResult> GetPaymentStatusAsync(string transactionId)
    {
        _logger.LogInformation("Checking ParamPOS payment status for {TransactionId}", transactionId);

        return Task.FromResult(new PaymentResult
        {
            IsSuccess = true,
            Status = PaymentStatus.Pending,
            TransactionId = transactionId
        });
    }

    private async Task<PaymentResult> HandleThreeDSecureReturnAsync(ParamPosSettings settings, Dictionary<string, string> form)
    {
        var md = GetValue(form, "md");
        var mdStatus = GetValue(form, "mdStatus");
        var orderId = GetValue(form, "orderId");
        var islemGuid = GetValue(form, "islemGUID");
        var islemHash = GetValue(form, "islemHash");

        if (string.IsNullOrWhiteSpace(md) || string.IsNullOrWhiteSpace(mdStatus) ||
            string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(islemGuid) || string.IsNullOrWhiteSpace(islemHash))
        {
            return new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = "Missing 3D callback parameters"
            };
        }

        var expectedHash = ComputeSha1Base64($"{islemGuid}{md}{mdStatus}{orderId}{settings.Guid}");
        if (!string.Equals(expectedHash, islemHash, StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentResult
            {
                IsSuccess = false,
                TransactionId = orderId,
                ErrorMessage = "3D hash verification failed",
                Status = PaymentStatus.Failed
            };
        }

        if (!IsThreeDSuccess(mdStatus))
        {
            return new PaymentResult
            {
                IsSuccess = false,
                TransactionId = orderId,
                ErrorMessage = "3D authentication failed",
                Status = PaymentStatus.Failed,
                ProviderData = new Dictionary<string, object>
                {
                    ["md_status"] = mdStatus
                }
            };
        }

        var body = new XElement(settings.XmlNamespace + "TP_WMD_Pay",
            BuildSecurityNode(settings),
            new XElement(settings.XmlNamespace + "GUID", settings.Guid),
            new XElement(settings.XmlNamespace + "UCD_MD", md),
            new XElement(settings.XmlNamespace + "Islem_GUID", islemGuid),
            new XElement(settings.XmlNamespace + "Siparis_ID", orderId)
        );

        var responseDoc = await SendSoapRequestAsync(settings, "TP_WMD_Pay", body);
        var sonuc = GetInt(responseDoc, "Sonuc");
        var sonucStr = GetString(responseDoc, "Sonuc_Ack") ?? GetString(responseDoc, "Sonuc_Str");
        var dekontId = GetString(responseDoc, "Dekont_ID");
        var bankTransId = GetString(responseDoc, "Bank_Trans_ID");
        var bankAuthCode = GetString(responseDoc, "Bank_AuthCode");

        if (sonuc <= 0)
        {
            return new PaymentResult
            {
                IsSuccess = false,
                TransactionId = orderId,
                Status = PaymentStatus.Failed,
                ErrorMessage = sonucStr ?? "Payment failed",
                ProviderData = new Dictionary<string, object>
                {
                    ["dekont_id"] = dekontId ?? string.Empty,
                    ["bank_trans_id"] = bankTransId ?? string.Empty,
                    ["bank_auth_code"] = bankAuthCode ?? string.Empty
                }
            };
        }

        return new PaymentResult
        {
            IsSuccess = true,
            TransactionId = orderId,
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTime.UtcNow,
            ProviderData = new Dictionary<string, object>
            {
                ["dekont_id"] = dekontId ?? string.Empty,
                ["bank_trans_id"] = bankTransId ?? string.Empty,
                ["bank_auth_code"] = bankAuthCode ?? string.Empty,
                ["md_status"] = mdStatus
            }
        };
    }

    private static PaymentResult HandleNonSecureReturn(ParamPosSettings settings, Dictionary<string, string> form)
    {
        var sonuc = GetValue(form, "TURKPOS_RETVAL_Sonuc");
        var siparisId = GetValue(form, "TURKPOS_RETVAL_Siparis_ID");
        var dekontId = GetValue(form, "TURKPOS_RETVAL_Dekont_ID");
        var islemId = GetValue(form, "TURKPOS_RETVAL_Islem_ID");
        var tahsilat = GetValue(form, "TURKPOS_RETVAL_Tahsilat_Tutari");
        var hash = GetValue(form, "TURKPOS_RETVAL_Hash");

        var expectedHash = ComputeSha1Base64($"{settings.ClientCode}{settings.Guid}{dekontId}{tahsilat}{siparisId}{islemId}");
        if (!string.IsNullOrWhiteSpace(hash) && !string.Equals(hash, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentResult
            {
                IsSuccess = false,
                TransactionId = siparisId,
                Status = PaymentStatus.Failed,
                ErrorMessage = "Return hash verification failed"
            };
        }

        var success = int.TryParse(sonuc, out var sonucInt) && sonucInt > 0;

        return new PaymentResult
        {
            IsSuccess = success,
            TransactionId = siparisId,
            Status = success ? PaymentStatus.Completed : PaymentStatus.Failed,
            ProcessedAt = success ? DateTime.UtcNow : null,
            ErrorMessage = success ? null : GetValue(form, "TURKPOS_RETVAL_Sonuc_Str"),
            ProviderData = new Dictionary<string, object>
            {
                ["dekont_id"] = dekontId ?? string.Empty,
                ["tahsilat_tutari"] = tahsilat ?? string.Empty
            }
        };
    }

    private async Task<XDocument> SendSoapRequestAsync(ParamPosSettings settings, string methodName, XElement body)
    {
        var envelope = new XDocument(
            new XElement(XName.Get("Envelope", "http://schemas.xmlsoap.org/soap/envelope/"),
                new XElement(XName.Get("Body", "http://schemas.xmlsoap.org/soap/envelope/"), body)
            )
        );

        var content = new StringContent(envelope.ToString(SaveOptions.DisableFormatting), Encoding.UTF8, "text/xml");
        var soapAction = $"{settings.SoapActionBase.TrimEnd('/')}/{methodName}";

        using var request = new HttpRequestMessage(HttpMethod.Post, settings.ServiceUrl);
        request.Headers.Add("SOAPAction", soapAction);
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("ParamPOS SOAP call failed: {StatusCode} {Method}", response.StatusCode, methodName);
            throw new InvalidOperationException($"ParamPOS SOAP call failed ({response.StatusCode})");
        }

        return XDocument.Parse(responseContent);
    }

    private XElement BuildSecurityNode(ParamPosSettings settings)
    {
        return new XElement(settings.XmlNamespace + "G",
            new XElement(settings.XmlNamespace + "CLIENT_CODE", settings.ClientCode),
            new XElement(settings.XmlNamespace + "CLIENT_USERNAME", settings.ClientUsername),
            new XElement(settings.XmlNamespace + "CLIENT_PASSWORD", settings.ClientPassword));
    }

    private static Dictionary<string, string> ParsePayload(string payload)
    {
        var query = QueryHelpers.ParseQuery(payload ?? string.Empty);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in query)
        {
            result[kvp.Key] = kvp.Value.ToString();
        }

        return result;
    }

    private static string GetValue(Dictionary<string, string> form, string key)
    {
        return form.TryGetValue(key, out var value) ? value : string.Empty;
    }

    private static bool IsThreeDSuccess(string mdStatus)
    {
        return mdStatus is "1" or "2" or "3" or "4";
    }

    private static string ComputeSha256Base64(string raw)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(raw);
        return Convert.ToBase64String(sha.ComputeHash(bytes));
    }

    private static string ComputeSha1Base64(string raw)
    {
        using var sha = SHA1.Create();
        var bytes = Encoding.UTF8.GetBytes(raw);
        return Convert.ToBase64String(sha.ComputeHash(bytes));
    }

    private static string FormatAmount(decimal amount)
    {
        return amount.ToString("F2", CultureInfo.GetCultureInfo("tr-TR"));
    }

    private static string NormalizeCardNumber(string cardNumber)
    {
        return new string(cardNumber.Where(char.IsDigit).ToArray());
    }

    private static string NormalizeMonth(string month)
    {
        var digits = new string(month.Where(char.IsDigit).ToArray());
        return digits.Length == 1 ? $"0{digits}" : digits;
    }

    private static string NormalizeYear(string year)
    {
        var digits = new string(year.Where(char.IsDigit).ToArray());
        if (digits.Length == 2)
        {
            return $"20{digits}";
        }
        return digits;
    }

    private static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("90") && digits.Length > 10)
        {
            digits = digits.Substring(digits.Length - 10);
        }
        else if (digits.StartsWith("0") && digits.Length > 10)
        {
            digits = digits.Substring(1);
        }

        return digits.Length > 10 ? digits.Substring(digits.Length - 10) : digits;
    }

    private static string GenerateOrderId(int workspaceId)
    {
        var orderId = $"YLP-{workspaceId}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        return orderId.Length > 48 ? orderId[..48] : orderId;
    }

    private static int GetInt(XDocument doc, string localName)
    {
        var value = GetString(doc, localName);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    private static string? GetString(XDocument doc, string localName)
    {
        return doc.Descendants().FirstOrDefault(e => string.Equals(e.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    private ParamPosSettings GetSettings()
    {
        var testMode = _configuration.GetValue<bool>("Payment:ParamPOS:TestMode");
        var serviceUrl = _configuration["Payment:ParamPOS:ServiceUrl"];

        if (string.IsNullOrWhiteSpace(serviceUrl))
        {
            serviceUrl = testMode
                ? _configuration["Payment:ParamPOS:ServiceUrlTest"]
                : _configuration["Payment:ParamPOS:ServiceUrlLive"];
        }

        var apiBaseUrl = _configuration["AppUrl"] ?? _configuration["Tracking:ApiBaseUrl"] ?? string.Empty;
        var appBaseUrl = _configuration["Tracking:BaseUrl"] ?? apiBaseUrl;
        var xmlNamespace = _configuration["Payment:ParamPOS:XmlNamespace"] ?? DefaultXmlNamespace;
        var soapActionBase = _configuration["Payment:ParamPOS:SoapActionBase"] ?? xmlNamespace;

        var isValid = !string.IsNullOrWhiteSpace(serviceUrl) &&
                      !string.IsNullOrWhiteSpace(_configuration["Payment:ParamPOS:ClientCode"]) &&
                      !string.IsNullOrWhiteSpace(_configuration["Payment:ParamPOS:ClientUsername"]) &&
                      !string.IsNullOrWhiteSpace(_configuration["Payment:ParamPOS:ClientPassword"]) &&
                      !string.IsNullOrWhiteSpace(_configuration["Payment:ParamPOS:Guid"]) &&
                      !string.IsNullOrWhiteSpace(apiBaseUrl);

        return new ParamPosSettings
        {
            ClientCode = _configuration["Payment:ParamPOS:ClientCode"] ?? string.Empty,
            ClientUsername = _configuration["Payment:ParamPOS:ClientUsername"] ?? string.Empty,
            ClientPassword = _configuration["Payment:ParamPOS:ClientPassword"] ?? string.Empty,
            Guid = _configuration["Payment:ParamPOS:Guid"] ?? string.Empty,
            ServiceUrl = serviceUrl ?? string.Empty,
            SoapActionBase = soapActionBase,
            XmlNamespace = XNamespace.Get(xmlNamespace),
            ApiBaseUrl = apiBaseUrl,
            AppBaseUrl = appBaseUrl,
            FallbackClientIp = "127.0.0.1",
            IsValid = isValid
        };
    }

    private static string BuildReturnUrl(string apiBaseUrl, string result)
    {
        var baseUrl = string.IsNullOrWhiteSpace(apiBaseUrl) ? string.Empty : apiBaseUrl.TrimEnd('/');
        return $"{baseUrl}/api/payment/parampos/return?result={result}";
    }

    private sealed class ParamPosSettings
    {
        public string ClientCode { get; init; } = string.Empty;
        public string ClientUsername { get; init; } = string.Empty;
        public string ClientPassword { get; init; } = string.Empty;
        public string Guid { get; init; } = string.Empty;
        public string ServiceUrl { get; init; } = string.Empty;
        public string SoapActionBase { get; init; } = DefaultXmlNamespace;
        public XNamespace XmlNamespace { get; init; } = XNamespace.Get(DefaultXmlNamespace);
        public string ApiBaseUrl { get; init; } = string.Empty;
        public string AppBaseUrl { get; init; } = string.Empty;
        public string FallbackClientIp { get; init; } = "127.0.0.1";
        public bool IsValid { get; init; }
    }
}
