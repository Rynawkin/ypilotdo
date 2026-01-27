using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Notifications;

public class MarkAllNotificationsAsReadCommand : BaseAuthenticatedCommand<Unit>
{
    public override bool RequiresDriver => true;
    public override bool RequiresDispatcher => false;
    public override bool RequiresAdmin => false;
    public override bool RequiresSuperAdmin => false;
    public override bool RequiresWorkspaceAccess => false;
}

public class MarkAllNotificationsAsReadCommandHandler : BaseAuthenticatedCommandHandler<MarkAllNotificationsAsReadCommand, Unit>
{
    private readonly AppDbContext _context;

    public MarkAllNotificationsAsReadCommandHandler(IUserService userService, AppDbContext context) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<Unit> HandleCommand(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = User.Id;
        
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead();
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}