using System.Text.Json.Serialization;
using FluentValidation;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Members;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Members;

/// <summary>
/// Update member role as an admin or dispatcher
/// </summary>
public class UpdateMemberRoleCommand : BaseAuthenticatedCommand<UserResponse>
{
    [JsonIgnore] public override bool RequiresAdmin => true;
    [JsonIgnore] public Guid UserId { get; set; }

    public MemberRole[] Roles { get; set; }
}

public class UpdateMemberRoleCommandValidator : AbstractValidator<UpdateMemberRoleCommand>;

public class UpdateMemberRoleCommandHandler(AppDbContext context, IUserService userService)
    : BaseAuthenticatedCommandHandler<UpdateMemberRoleCommand, UserResponse>(userService)
{
    override protected async Task<UserResponse> HandleCommand(UpdateMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var member = await UserService.GetUserAsync(request.UserId, User.WorkspaceId, cancellationToken);
        if (member == null)
            throw new ApiException("Member not found", 404);
        
        await UserService.UpdateRoleAsync(User.Id, member, request.Roles, cancellationToken);
        
        context.Users.Update(member);
        await context.SaveChangesAsync(cancellationToken);

        return new UserResponse(member);
    }
}