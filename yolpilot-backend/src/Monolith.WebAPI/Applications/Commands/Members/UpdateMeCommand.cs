using System.Text.Json.Serialization;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Members;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Members;

public class UpdateMeCommand : BaseAuthenticatedCommand<UserResponse>
{
    [JsonIgnore] public override bool RequiresWorkspaceAccess => true;

    public string FullName { get; set; }
    public string LicenseNumber { get; set; }
}

public class UpdateUserCommandHandler(AppDbContext context, IUserService userService)
    : BaseAuthenticatedCommandHandler<UpdateMeCommand, UserResponse>(userService)
{
    override protected async Task<UserResponse> HandleCommand(UpdateMeCommand request, CancellationToken cancellationToken)
    {
        User.SetFullName(request.FullName);
        User.SetLicenseNumber(request.LicenseNumber);

        context.Users.Update(User);
        await context.SaveChangesAsync(cancellationToken);
        return new UserResponse(User);
    }
}