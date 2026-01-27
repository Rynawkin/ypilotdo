using System.Text.Json.Serialization;
using FluentValidation;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Members;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Members;

/// <summary>
/// Update member depot as an admin or dispatcher
/// </summary>
public class UpdateMemberDepotCommand : BaseAuthenticatedCommand<UserResponse>
{
    [JsonIgnore] public override bool RequiresAdmin => true;
    [JsonIgnore] public Guid UserId { get; set; }
    public int DepotId { get; set; }
}

public class UpdateMemberDepotCommandValidator : AbstractValidator<UpdateMemberDepotCommand>
{
    public UpdateMemberDepotCommandValidator()
    {
        RuleFor(x => x.DepotId).NotEmpty();
    }
}

public class UpdateMemberDepotHandler(AppDbContext context, IUserService userService)
    : BaseAuthenticatedCommandHandler<UpdateMemberDepotCommand, UserResponse>(userService)
{
    override protected async Task<UserResponse> HandleCommand(UpdateMemberDepotCommand request, CancellationToken cancellationToken)
    {
        var member = await UserService.GetUserAsync(request.UserId, User.WorkspaceId, cancellationToken);
        if (member == null)
            throw new ApiException("Member not found", 404);

        await UserService.UpdateDepotAsync(member, request.DepotId, User.WorkspaceId, cancellationToken);
        
        context.Users.Update(member);
        await context.SaveChangesAsync(cancellationToken);

        return new UserResponse(member);
    }
}