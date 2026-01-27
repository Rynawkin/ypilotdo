using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Workspace;

public class UpdateSavedLocationCommand : BaseAuthenticatedCommand<SavedLocationResponse>
{
    public override bool RequiresDispatcher => true;
    
    [JsonIgnore] public int SavedLocationId { get; set; }

    public string Name { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public SavedLocation ToEntity(SavedLocation savedLocation)
    {
        savedLocation.Update(this);
        return savedLocation;
    }
}

public class UpdateSavedLocationCommandValidator : AbstractValidator<UpdateSavedLocationCommand>
{
    public UpdateSavedLocationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Address).NotEmpty();
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}

public class UpdateSavedLocationCommandHandler(AppDbContext context, IUserService userService)
    : BaseAuthenticatedCommandHandler<UpdateSavedLocationCommand, SavedLocationResponse>(userService)
{
    override protected async Task<SavedLocationResponse> HandleCommand(UpdateSavedLocationCommand request,
        CancellationToken cancellationToken)
    {
        var savedLocation = await context.SavedLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.SavedLocationId && x.WorkspaceId == User.WorkspaceId,
                cancellationToken);
        if (savedLocation == null)
            throw new ApiException("Saved location not found", 404);

        savedLocation = request.ToEntity(savedLocation);
        context.SavedLocations.Update(savedLocation);
        await context.SaveChangesAsync(cancellationToken);

        return new SavedLocationResponse(savedLocation);
    }
}