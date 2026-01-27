using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Notifications;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Notifications;

public class GetUnreadNotificationCountQuery : BaseAuthenticatedCommand<UnreadCountResponse>
{
    public override bool RequiresDriver => true;
    public override bool RequiresDispatcher => false;
    public override bool RequiresAdmin => false;
    public override bool RequiresSuperAdmin => false;
    public override bool RequiresWorkspaceAccess => false;
}

public class GetUnreadNotificationCountQueryHandler : BaseAuthenticatedCommandHandler<GetUnreadNotificationCountQuery, UnreadCountResponse>
{
    private readonly AppDbContext _context;

    public GetUnreadNotificationCountQueryHandler(IUserService userService, AppDbContext context) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<UnreadCountResponse> HandleCommand(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
    {
        var userId = User.Id;
        
        var unreadCount = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync(cancellationToken);

        return new UnreadCountResponse
        {
            UnreadCount = unreadCount
        };
    }
}