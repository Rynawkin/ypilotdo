using System.Text.Json.Serialization;
using FluentValidation;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Workspace;

public class CreateDepotCommand : BaseAuthenticatedCommand<DepotResponse>
{
    public override bool RequiresDispatcher => true;

    public string Name { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public TimeSpan StartWorkingHours { get; set; }
    public TimeSpan? EndWorkingHours { get; set; }

    public Depot ToEntity(int workspaceId) => new(this, workspaceId);
}

public class CreateDepotCommandValidator : AbstractValidator<CreateDepotCommand>
{
    public CreateDepotCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.StartWorkingHours).InclusiveBetween(TimeSpan.Zero, TimeSpan.FromHours(24));
        RuleFor(x => x.EndWorkingHours).InclusiveBetween(TimeSpan.Zero, TimeSpan.FromHours(24)).When(x => x.EndWorkingHours.HasValue);
    }
}

public class CreateDepotCommandHandler(AppDbContext context, IUserService userService)
    : BaseAuthenticatedCommandHandler<CreateDepotCommand, DepotResponse>(userService)
{
    override protected async Task<DepotResponse> HandleCommand(CreateDepotCommand request, CancellationToken cancellationToken)
    {
        var depot = request.ToEntity(User.WorkspaceId);
        await context.Depots.AddAsync(depot, cancellationToken);
        if (User.DepotId == null)
        {
            User.SetDepot(depot);
            context.Users.Update(User);
        }

        await context.SaveChangesAsync(cancellationToken);

        return new DepotResponse(depot);
    }
}