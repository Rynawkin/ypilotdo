using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Services.Payment;
using Monolith.WebAPI.Services.Subscription;

namespace Monolith.WebAPI.Applications.Commands.Payment;

public class UpgradePlanCommand : BaseAuthenticatedCommand<UpgradePlanResponse>
{
    public PlanType NewPlanType { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? SuccessUrl { get; set; }
    public string? FailUrl { get; set; }
    public string? ClientIp { get; set; }
    public string? ReferrerUrl { get; set; }
    public PaymentCard? Card { get; set; }
    
    public override bool RequiresDriver => false;
}

public class UpgradePlanResponse
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PaymentUrl { get; set; }
    public string? TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
}

public class UpgradePlanCommandHandler : BaseAuthenticatedCommandHandler<UpgradePlanCommand, UpgradePlanResponse>
{
    private readonly IPaymentService _paymentService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly AppDbContext _context;

    public UpgradePlanCommandHandler(
        IPaymentService paymentService, 
        ISubscriptionService subscriptionService,
        AppDbContext context,
        IUserService userService)
        : base(userService)
    {
        _paymentService = paymentService;
        _subscriptionService = subscriptionService;
        _context = context;
    }

    protected override async Task<UpgradePlanResponse> HandleCommand(UpgradePlanCommand request, CancellationToken cancellationToken)
    {
        // Admin veya workspace owner kontrolü
        if (!User.IsAdmin && !User.IsSuperAdmin)
        {
            throw new ApiException("Only administrators can upgrade plan", 403);
        }

        // Plan geçerli mi kontrol et
        if (request.NewPlanType == PlanType.Trial)
        {
            return new UpgradePlanResponse
            {
                IsSuccess = false,
                ErrorMessage = "Cannot upgrade to Trial plan"
            };
        }

        // Workspace bilgilerini al
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == User.WorkspaceId);
            
        if (workspace == null)
        {
            return new UpgradePlanResponse
            {
                IsSuccess = false,
                ErrorMessage = "Workspace not found"
            };
        }

        // Plan limitlerini al
        var planLimits = _subscriptionService.GetPlanLimits(request.NewPlanType);
        
        // Ödeme isteği oluştur
        var paymentRequest = new PaymentRequest
        {
            WorkspaceId = User.WorkspaceId,
            Amount = planLimits.MonthlyPrice,
            Currency = "TRY",
            Description = $"{request.NewPlanType} Plan Upgrade",
            CustomerEmail = request.CustomerEmail,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            SuccessUrl = request.SuccessUrl,
            FailUrl = request.FailUrl,
            ClientIp = request.ClientIp,
            ReferrerUrl = request.ReferrerUrl,
            Card = request.Card,
            PlanType = request.NewPlanType,
            ExtraData = new Dictionary<string, object>
            {
                ["workspace_id"] = User.WorkspaceId,
                ["user_id"] = User.Id,
                ["upgrade_from"] = workspace.PlanType.ToString()
            }
        };

        var result = await _paymentService.InitiatePaymentAsync(paymentRequest);

        if (result.IsSuccess)
        {
            // Fatura oluştur
            var invoice = await _paymentService.CreateInvoiceAsync(
                User.WorkspaceId,
                request.NewPlanType,
                planLimits.MonthlyPrice,
                result.InternalTransactionId);

            if (result.Status == PaymentStatus.Completed)
            {
                workspace.UpdatePlan(request.NewPlanType);
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return new UpgradePlanResponse
            {
                IsSuccess = true,
                PaymentUrl = result.PaymentUrl,
                TransactionId = result.TransactionId,
                Amount = planLimits.MonthlyPrice,
                Currency = "TRY"
            };
        }

        return new UpgradePlanResponse
        {
            IsSuccess = false,
            ErrorMessage = result.ErrorMessage
        };
    }
}
