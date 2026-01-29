using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands.Payment;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Extensions;
using Monolith.WebAPI.Services.Payment;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Monolith.WebAPI.Controllers;

[Route("api/payment")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
[SwaggerControllerOrder(10)]
public class PaymentController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IPaymentService _paymentService;
    private readonly ITrialService _trialService;
    private readonly ILogger<PaymentController> _logger;
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public PaymentController(
        ISender sender, 
        IPaymentService paymentService,
        ITrialService trialService,
        ILogger<PaymentController> logger,
        AppDbContext context,
        IConfiguration configuration)
    {
        _sender = sender;
        _paymentService = paymentService;
        _trialService = trialService;
        _logger = logger;
        _context = context;
        _configuration = configuration;
    }

    /// <summary>
    /// Start trial period for workspace
    /// </summary>
    [HttpPost("start-trial")]
    [SwaggerOperation(Summary = "Start 14-day trial period")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<StartTrialResponse>> StartTrial()
    {
        var command = new StartTrialCommand
        {
            AuthenticatedUserId = User.GetId()
        };

        var result = await _sender.Send(command);
        
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Initiate plan upgrade payment
    /// </summary>
    [HttpPost("initiate-upgrade")]
    [SwaggerOperation(Summary = "Initiate payment for plan upgrade")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<UpgradePlanResponse>> InitiateUpgrade([FromBody] UpgradePlanRequest request)
    {
        var clientIp = GetClientIp();
        var referrerUrl = request.ReferrerUrl;
        if (string.IsNullOrWhiteSpace(referrerUrl))
        {
            referrerUrl = Request.Headers["Origin"].FirstOrDefault() ??
                          Request.Headers["Referer"].FirstOrDefault();
        }

        var command = new UpgradePlanCommand
        {
            AuthenticatedUserId = User.GetId(),
            NewPlanType = request.PlanType,
            CustomerEmail = request.CustomerEmail,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            SuccessUrl = request.SuccessUrl,
            FailUrl = request.FailUrl,
            ClientIp = clientIp,
            ReferrerUrl = referrerUrl,
            Card = request.Card
        };

        var result = await _sender.Send(command);
        
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// PayTR webhook endpoint
    /// </summary>
    [HttpPost("webhook/paytr")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "PayTR payment webhook")]
    public async Task<IActionResult> PayTRWebhook()
    {
        var payload = await ReadRequestBodyAsync();
        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());

        var command = new ProcessPaymentWebhookCommand
        {
            Provider = "PayTR",
            Payload = payload,
            Headers = headers
        };

        var result = await _sender.Send(command);
        
        if (result.IsSuccess)
        {
            return Ok("OK");
        }

        return BadRequest("Failed");
    }

    /// <summary>
    /// ParamPOS webhook endpoint
    /// </summary>
    [HttpPost("webhook/parampos")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "ParamPOS payment webhook")]
    public async Task<IActionResult> ParamPOSWebhook()
    {
        var payload = await ReadRequestBodyAsync();
        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());

        var command = new ProcessPaymentWebhookCommand
        {
            Provider = "ParamPOS",
            Payload = payload,
            Headers = headers
        };

        var result = await _sender.Send(command);
        
        if (result.IsSuccess)
        {
            return Ok("OK");
        }

        return BadRequest("Failed");
    }

    /// <summary>
    /// ParamPOS 3D redirect HTML
    /// </summary>
    [HttpGet("parampos/3d/{orderId}")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "ParamPOS 3D redirect HTML")]
    public async Task<IActionResult> ParamPOS3dRedirect(string orderId)
    {
        var transaction = await _context.PaymentTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ProviderTransactionId == orderId);

        if (transaction == null)
        {
            return NotFound("Transaction not found");
        }

        var data = DeserializeProviderData(transaction.ProviderResponse);
        var ucdHtml = GetProviderValue(data, "ucd_html");

        if (string.IsNullOrWhiteSpace(ucdHtml))
        {
            return BadRequest("Missing 3D HTML");
        }

        return Content(ucdHtml, "text/html");
    }

    /// <summary>
    /// ParamPOS 3D return handler (success/fail)
    /// </summary>
    [HttpPost("parampos/return")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "ParamPOS 3D return handler")]
    public Task<IActionResult> ParamPOSReturnPost() => HandleParamPosReturnAsync();

    [HttpGet("parampos/return")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "ParamPOS 3D return handler (GET)")]
    public Task<IActionResult> ParamPOSReturnGet() => HandleParamPosReturnAsync();

    /// <summary>
    /// Get workspace invoices
    /// </summary>
    [HttpGet("invoices")]
    [SwaggerOperation(Summary = "Get workspace invoices")]
    public async Task<ActionResult<List<InvoiceResponse>>> GetInvoices()
    {
        var workspaceId = User.GetWorkspaceId();
        var invoices = await _paymentService.GetInvoicesAsync(workspaceId);

        var response = invoices.Select(i => new InvoiceResponse
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            Amount = i.Amount,
            Tax = i.Tax,
            Total = i.Total,
            DueDate = i.DueDate,
            PaidDate = i.PaidDate,
            Status = i.Status,
            PlanType = i.PlanType,
            PeriodStart = i.PeriodStart,
            PeriodEnd = i.PeriodEnd,
            CreatedAt = i.CreatedAt
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Get trial status
    /// </summary>
    [HttpGet("trial-status")]
    [SwaggerOperation(Summary = "Get trial status and remaining days")]
    public async Task<ActionResult<TrialStatusResponse>> GetTrialStatus()
    {
        var workspaceId = User.GetWorkspaceId();
        var status = await _trialService.GetTrialStatusAsync(workspaceId);

        var response = new TrialStatusResponse
        {
            IsActive = status.IsActive,
            IsExpired = status.IsExpired,
            StartDate = status.StartDate,
            EndDate = status.EndDate,
            RemainingDays = status.RemainingDays,
            Limits = status.Limits
        };

        return Ok(response);
    }

    private async Task<string> ReadRequestBodyAsync()
    {
        using var reader = new StreamReader(Request.Body);
        return await reader.ReadToEndAsync();
    }

    private async Task<IActionResult> HandleParamPosReturnAsync()
    {
        var payload = await ReadRequestBodyOrQueryAsync();
        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());

        var command = new ProcessPaymentWebhookCommand
        {
            Provider = "ParamPOS",
            Payload = payload,
            Headers = headers
        };

        var result = await _sender.Send(command);
        var success = result.IsSuccess && result.Status == PaymentStatus.Completed;
        var redirectUrl = await ResolveRedirectUrlAsync(result.TransactionId, success);
        var html = BuildRedirectHtml(redirectUrl, success);

        return Content(html, "text/html");
    }

    private async Task<string> ReadRequestBodyOrQueryAsync()
    {
        var body = await ReadRequestBodyAsync();
        if (!string.IsNullOrWhiteSpace(body))
        {
            return body;
        }

        return Request.QueryString.Value?.TrimStart('?') ?? string.Empty;
    }

    private async Task<string> ResolveRedirectUrlAsync(string? transactionId, bool success)
    {
        var fallbackBase = _configuration["Tracking:BaseUrl"] ?? _configuration["AppUrl"] ?? string.Empty;
        var fallback = $"{fallbackBase.TrimEnd('/')}/payment/{(success ? "success" : "failed")}";

        if (string.IsNullOrWhiteSpace(transactionId))
        {
            return fallback;
        }

        var transaction = await _context.PaymentTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ProviderTransactionId == transactionId);

        if (transaction == null)
        {
            return fallback;
        }

        var data = DeserializeProviderData(transaction.ProviderResponse);
        var urlKey = success ? "success_url" : "fail_url";
        var url = GetProviderValue(data, urlKey);

        return string.IsNullOrWhiteSpace(url) ? fallback : url;
    }

    private static string BuildRedirectHtml(string redirectUrl, bool success)
    {
        var safeUrl = HtmlEncoder.Default.Encode(redirectUrl);
        var statusText = success ? "Payment completed" : "Payment failed";

        return $@"<!DOCTYPE html>
<html>
  <head>
    <meta charset=""utf-8"" />
    <meta http-equiv=""refresh"" content=""0;url={safeUrl}"" />
    <title>{statusText}</title>
  </head>
  <body>
    <p>{statusText}. Redirecting...</p>
    <script>window.location.href = '{JavaScriptEncoder.Default.Encode(redirectUrl)}';</script>
  </body>
</html>";
    }

    private static Dictionary<string, object>? DeserializeProviderData(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetProviderValue(Dictionary<string, object>? data, string key)
    {
        if (data == null || !data.TryGetValue(key, out var value))
        {
            return null;
        }

        if (value is JsonElement element)
        {
            return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
        }

        return value?.ToString();
    }

    private string? GetClientIp()
    {
        var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',').FirstOrDefault()?.Trim();
        }

        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
        {
            return realIp.Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}

// Request/Response DTOs
public class UpgradePlanRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PlanType PlanType { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? SuccessUrl { get; set; }
    public string? FailUrl { get; set; }
    public string? ReferrerUrl { get; set; }
    public PaymentCard? Card { get; set; }
}

public class InvoiceResponse
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public InvoiceStatus Status { get; set; }
    public PlanType PlanType { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TrialStatusResponse
{
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int RemainingDays { get; set; }
    public TrialLimits? Limits { get; set; }
}
