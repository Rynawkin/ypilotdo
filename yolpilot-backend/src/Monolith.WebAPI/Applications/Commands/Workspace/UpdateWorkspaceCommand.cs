using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Workspace;

public class UpdateWorkspaceCommand : BaseAuthenticatedCommand<WorkspaceResponse>
{
    public override bool RequiresAdmin => true;
    
    public string Name { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public string DistanceUnit { get; set; }
    public string Currency { get; set; }
    public string TimeZone { get; set; }
    public string Language { get; set; }
    public double? CostPerKm { get; set; }
    public double? CostPerHour { get; set; }

    public TimeSpan DefaultServiceTime { get; set; }

    public Data.Workspace.Workspace ToEntity(Data.Workspace.Workspace workspace)
    {
        workspace.Update(this);
        return workspace;
    }
}

public class UpdateWorkspaceCommandValidator : AbstractValidator<UpdateWorkspaceCommand>
{
    public UpdateWorkspaceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.PhoneNumber).NotEmpty();
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.DistanceUnit).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty();
        RuleFor(x => x.TimeZone).NotEmpty();
        RuleFor(x => x.Language)
            .NotEmpty().WithMessage("Language is required.")
            .Must(LanguageHelper.IsValidLanguage)
            .WithMessage($"Invalid language code. Valid language codes are: {string.Join(", ", LanguageHelper.ValidLanguages)}");
    }
}

public class UpdateWorkspaceCommandHandler(AppDbContext context, IUserService userService)
    : BaseAuthenticatedCommandHandler<UpdateWorkspaceCommand, WorkspaceResponse>(userService)
{
    override protected async Task<WorkspaceResponse> HandleCommand(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await context.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == User.WorkspaceId, cancellationToken);
        if (workspace == null) throw new ApiException("Workspace not found.", 404);

        workspace = request.ToEntity(workspace);
        context.Workspaces.Update(workspace);
        await context.SaveChangesAsync(cancellationToken);

        return new WorkspaceResponse(workspace);
    }
}