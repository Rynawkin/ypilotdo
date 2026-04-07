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
    private readonly IPaymentProvisioningService _paymentProvisioningService;
    private readonly ILogger<ProcessPaymentWebhookCommandHandler> _logger;
    private readonly AppDbContext _context;

    public ProcessPaymentWebhookCommandHandler(
        IPaymentService paymentService,
        IPaymentProvisioningService paymentProvisioningService,
        ILogger<ProcessPaymentWebhookCommandHandler> logger,
        AppDbContext context)
    {
        _paymentService = paymentService;
        _paymentProvisioningService = paymentProvisioningService;
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
                var transaction = await _context.PaymentTransactions
                    .Include(t => t.Workspace)
                    .FirstOrDefaultAsync(t => t.ProviderTransactionId == result.TransactionId, cancellationToken);

                if (transaction != null)
                {
                    await _paymentProvisioningService.EnsureProvisionedAsync(transaction, cancellationToken);
                }
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
}
