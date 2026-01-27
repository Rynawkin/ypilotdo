using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Services.Workspace;

namespace Monolith.WebAPI.Applications.Commands.Members;

public enum MemberRole
{
    Admin = 1,
    Dispatcher = 10,
    Driver = 20,
}

public class CreateTempMemberCommand : BaseAuthenticatedCommand<string>
{
    [JsonIgnore] public override bool RequiresAdmin => true;
    
    public int DepotId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public MemberRole[] Roles { get; set; }

    public TempMember ToEntity(ApplicationUser inviter, Data.Workspace.Workspace workspace, Depot depot) =>
        new(this, inviter, workspace, depot);
}

public class AddMemberCommandValidator : AbstractValidator<CreateTempMemberCommand>
{
    public AddMemberCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FullName).NotEmpty();
    }
}

public class AddMemberCommandHandler(AppDbContext context, IUserService userService, IWorkspaceService workspaceService)
    : BaseAuthenticatedCommandHandler<CreateTempMemberCommand, string>(userService)
{
    override protected async Task<string> HandleCommand(CreateTempMemberCommand request, CancellationToken cancellationToken)
    {
        if (request.Roles.Contains(MemberRole.Driver))
            await workspaceService.ThrowExceptionIfReachedToMaxDriverCountAsync(User.WorkspaceId, cancellationToken);

        var existingUser = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);
        if (existingUser != null)
            throw new ApiException("User already exists", 400);

        var existingInvite = await context.TempMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);
        if (existingInvite != null)
        {
            //todo send email to the new member
            return existingInvite.Token.ToString();
        }

        var depot = await context.Depots.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.DepotId
                                                                                 && x.WorkspaceId == User.WorkspaceId, cancellationToken);
        if (depot == null)
            throw new ApiException("Depot not found", 404);

        var tempMember = request.ToEntity(User, User.Workspace, depot);
        await context.TempMembers.AddAsync(tempMember, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // todo send email to the new member
        return tempMember.Token.ToString();
    }
}