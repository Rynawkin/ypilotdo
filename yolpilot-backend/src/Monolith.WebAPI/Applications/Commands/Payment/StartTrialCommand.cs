using MediatR;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Services.Payment;

namespace Monolith.WebAPI.Applications.Commands.Payment;

public class StartTrialCommand : BaseAuthenticatedCommand<StartTrialResponse>
{
    public override bool RequiresDriver => false;
}

public class StartTrialResponse
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? TrialStartDate { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public int RemainingDays { get; set; }
    public TrialLimits? Limits { get; set; }
}

public class StartTrialCommandHandler : BaseAuthenticatedCommandHandler<StartTrialCommand, StartTrialResponse>
{
    private readonly ITrialService _trialService;

    public StartTrialCommandHandler(ITrialService trialService, IUserService userService)
        : base(userService)
    {
        _trialService = trialService;
    }

    protected override async Task<StartTrialResponse> HandleCommand(StartTrialCommand request, CancellationToken cancellationToken)
    {
        // Admin veya workspace owner kontrolü
        if (!User.IsAdmin && !User.IsSuperAdmin)
        {
            throw new ApiException("Only administrators can start trial", 403);
        }

        var canStart = await _trialService.CanStartTrialAsync(User.WorkspaceId);
        if (!canStart)
        {
            return new StartTrialResponse
            {
                IsSuccess = false,
                ErrorMessage = "Trial cannot be started. Either already used or paid plan is active."
            };
        }

        var result = await _trialService.StartTrialAsync(User.WorkspaceId);
        
        if (result.IsSuccess)
        {
            var status = await _trialService.GetTrialStatusAsync(User.WorkspaceId);
            
            return new StartTrialResponse
            {
                IsSuccess = true,
                TrialStartDate = result.TrialStartDate,
                TrialEndDate = result.TrialEndDate,
                RemainingDays = result.RemainingDays,
                Limits = status.Limits
            };
        }

        return new StartTrialResponse
        {
            IsSuccess = false,
            ErrorMessage = result.ErrorMessage
        };
    }
}