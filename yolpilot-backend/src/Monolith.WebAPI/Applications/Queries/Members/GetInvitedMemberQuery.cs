using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Members;

public record GetInvitedMemberQuery(int WorkspaceId, Guid Token) : IRequest<object>;

public class GetInvitedMemberQueryHandler(AppDbContext context, IUserService userService) : IRequestHandler<GetInvitedMemberQuery, object>
{
    public async Task<object> Handle(GetInvitedMemberQuery request, CancellationToken cancellationToken)
    {
        var member = await context.TempMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Token == request.Token && !x.IsSaved, cancellationToken: cancellationToken);
        if (member == null)
            throw new ApiException("Token is invalid", 400);

        return new
        {
            member.Email,
            member.FullName,
            inviter = await userService.GetUserAsync(new Guid(member.InviterId), cancellationToken)
        };
    }
}