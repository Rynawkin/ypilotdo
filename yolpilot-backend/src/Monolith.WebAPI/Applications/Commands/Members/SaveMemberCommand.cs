using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Services.Workspace;

namespace Monolith.WebAPI.Applications.Commands.Members;

public class SaveMemberCommand : IRequest<TokenModel>
{
    public string Password { get; set; }
    [JsonIgnore] public Guid Token { get; set; }
}

public class SaveMemberCommandValidator : AbstractValidator<SaveMemberCommand>
{
    public SaveMemberCommandValidator()
    {
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.Token).NotEmpty();
    }
}

public class SaveMemberCommandHandler(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IWorkspaceService workspaceService)
    : IRequestHandler<SaveMemberCommand, TokenModel>
{
    public async Task<TokenModel> Handle(SaveMemberCommand request, CancellationToken cancellationToken)
    {
        var tempMember = await context.TempMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Token == request.Token && !x.IsSaved, cancellationToken: cancellationToken);
        if (tempMember == null)
            throw new ApiException("Token is invalid", 400);
        
        if (tempMember.Roles.Contains((int)MemberRole.Driver))
            await workspaceService.ThrowExceptionIfReachedToMaxDriverCountAsync(tempMember.WorkspaceId, cancellationToken);

        var user = new ApplicationUser(tempMember);
        var creatingUserResult = await userManager.CreateAsync(user, request.Password);
        if (creatingUserResult.Succeeded is false)
        {
            var errors = creatingUserResult.Errors.Select(x => x.Description);
            var errorMessage = string.Join(", ", errors);
            throw new ApiException(errorMessage, 400);
        }

        tempMember.Save(await userManager.FindByIdAsync(tempMember.InviterId),
            await context.Workspaces
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == tempMember.WorkspaceId, cancellationToken: cancellationToken));

        context.TempMembers.Update(tempMember);
        await context.SaveChangesAsync(cancellationToken);

        return tokenService.Create(user);
    }
}