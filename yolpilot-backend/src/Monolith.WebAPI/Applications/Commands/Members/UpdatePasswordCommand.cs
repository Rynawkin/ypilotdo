using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Members;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Members;

public class UpdatePasswordCommand : BaseAuthenticatedCommand<UserResponse>
{
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;

    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
}

public class UpdatePasswordCommandHandler(UserManager<ApplicationUser> userManager, IUserService userService)
    : BaseAuthenticatedCommandHandler<UpdatePasswordCommand, UserResponse>(userService)
{
    override protected async Task<UserResponse> HandleCommand(UpdatePasswordCommand request, CancellationToken cancellationToken)
    {
        if (await userManager.CheckPasswordAsync(User!, request.CurrentPassword) is false)
            throw new ApiException("Current password is incorrect", 400);

        var changingPasswordResult = await userManager.ChangePasswordAsync(User!,
            request.CurrentPassword, request.NewPassword);
        if (changingPasswordResult.Succeeded is false)
        {
            var errors = changingPasswordResult.Errors.Select(x => x.Description);
            var errorMessage = string.Join(", ", errors);
            throw new ApiException(errorMessage, 400);
        }

        return new UserResponse(User!);
    }
}