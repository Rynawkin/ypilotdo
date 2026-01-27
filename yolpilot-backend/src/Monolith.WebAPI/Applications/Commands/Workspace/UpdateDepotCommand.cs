using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Workspace;

public class UpdateDepotCommand : BaseAuthenticatedCommand<DepotResponse>
{
    public override bool RequiresDispatcher => true;
    
    [JsonIgnore] public int DepotId { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public TimeSpan StartWorkingHours { get; set; }
    public TimeSpan? EndWorkingHours { get; set; }

    public Depot ToEntity(Depot depot)
    {
        depot.Update(this);
        return depot;
    }
}

public class UpdateDepotCommandValidator : AbstractValidator<UpdateDepotCommand>
{
    public UpdateDepotCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.StartWorkingHours).InclusiveBetween(TimeSpan.Zero, TimeSpan.FromHours(24));
        RuleFor(x => x.EndWorkingHours).InclusiveBetween(TimeSpan.Zero, TimeSpan.FromHours(24)).When(x => x.EndWorkingHours.HasValue);
    }
}

public class UpdateDepotCommandHandler(AppDbContext context, IUserService userService)
    : BaseAuthenticatedCommandHandler<UpdateDepotCommand, DepotResponse>(userService)
{
    override protected async Task<DepotResponse> HandleCommand(UpdateDepotCommand request, CancellationToken cancellationToken)
    {
        var depot = await context.Depots
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.DepotId && x.WorkspaceId == User.WorkspaceId, cancellationToken);
        if (depot == null)
            throw new ApiException("Depot not found", 404);

        depot.Update(request);
        context.Depots.Update(depot);
        await context.SaveChangesAsync(cancellationToken);

        return new DepotResponse(depot);
    }
}