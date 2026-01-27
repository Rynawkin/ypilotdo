using System.Text.Json.Serialization;
using FluentValidation;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Workspace;

public class AddMemberToDepotCommand : BaseAuthenticatedCommand<DepotResponse>
{
    public override bool RequiresAdmin => true;

    [JsonIgnore] public int DepotId { get; set; }
    public Guid UserId { get; set; }
}

public class AddMemberToDepotCommandValidator : AbstractValidator<AddMemberToDepotCommand>
{
    public AddMemberToDepotCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DepotId).NotEmpty();
        RuleFor(x => x.AuthenticatedUserId).NotEmpty();
    }
}

public class AddMemberToDepotCommandHandler(AppDbContext context, IUserService userService)
    : BaseAuthenticatedCommandHandler<AddMemberToDepotCommand, DepotResponse>(userService)
{
    override protected async Task<DepotResponse> HandleCommand(AddMemberToDepotCommand request, CancellationToken cancellationToken)
    {
        var member = await UserService.GetUserAsync(request.UserId, User.WorkspaceId, cancellationToken);
        if (member == null)
            throw new ApiException("User not found.", 404);

        var depot = await UserService.UpdateDepotAsync(member, request.DepotId, User.WorkspaceId, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        depot.Members.Add(member);
        return new DepotResponse(depot);
    }
}