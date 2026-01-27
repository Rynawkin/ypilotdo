using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Notifications;

public class ClearReadNotificationsCommand : BaseAuthenticatedCommand<Unit>
{
    public override bool RequiresDriver => true;
    public override bool RequiresDispatcher => false;
    public override bool RequiresAdmin => false;
    public override bool RequiresSuperAdmin => false;
    public override bool RequiresWorkspaceAccess => false;
}

public class ClearReadNotificationsCommandHandler : BaseAuthenticatedCommandHandler<ClearReadNotificationsCommand, Unit>
{
    private readonly AppDbContext _context;

    public ClearReadNotificationsCommandHandler(IUserService userService, AppDbContext context) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<Unit> HandleCommand(ClearReadNotificationsCommand request, CancellationToken cancellationToken)
    {
        var userId = User.Id;
        
        var readNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && n.IsRead)
            .ToListAsync(cancellationToken);

        // Hard delete - remove from database
        _context.Notifications.RemoveRange(readNotifications);

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}