using System.Text.Json.Serialization;
using FluentValidation;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Workspace;

public class CreateSavedLocationCommand : BaseAuthenticatedCommand<SavedLocationResponse>
{
    public override bool RequiresDispatcher => true;

    public string Name { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public SavedLocation ToEntity(int workspaceId) => new(this, workspaceId);
}

public class CreateSavedLocationCommandValidator : AbstractValidator<CreateSavedLocationCommand>
{
    public CreateSavedLocationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Address).NotEmpty();
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}

public class AddSavedLocationCommandValidator : AbstractValidator<CreateSavedLocationCommand>
{
    public AddSavedLocationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Address).NotEmpty();
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}

public class AddSavedLocationCommandHandler(AppDbContext context, IUserService userService)
    : BaseAuthenticatedCommandHandler<CreateSavedLocationCommand, SavedLocationResponse>(userService)
{
    override protected async Task<SavedLocationResponse> HandleCommand(CreateSavedLocationCommand request,
        CancellationToken cancellationToken)
    {
        var savedLocation = request.ToEntity(User.WorkspaceId);
        context.SavedLocations.Add(savedLocation);
        await context.SaveChangesAsync(cancellationToken);

        return new SavedLocationResponse(savedLocation);
    }
}