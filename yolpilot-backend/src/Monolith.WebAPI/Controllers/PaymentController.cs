using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monolith.WebAPI.Applications.Commands.Payment;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Extensions;
using Monolith.WebAPI.Services.Payment;
using Swashbuckle.AspNetCore.Annotations;

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

    public PaymentController(
        ISender sender, 
        IPaymentService paymentService,
        ITrialService trialService,
        ILogger<PaymentController> logger)
    {
        _sender = sender;
        _paymentService = paymentService;
        _trialService = trialService;
        _logger = logger;
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
        var command = new UpgradePlanCommand
        {
            AuthenticatedUserId = User.GetId(),
            NewPlanType = request.PlanType,
            CustomerEmail = request.CustomerEmail,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            SuccessUrl = request.SuccessUrl,
            FailUrl = request.FailUrl
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
}

// Request/Response DTOs
public class UpgradePlanRequest
{
    public PlanType PlanType { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? SuccessUrl { get; set; }
    public string? FailUrl { get; set; }
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