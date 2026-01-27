using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Notifications;

public class MarkNotificationAsReadCommand : BaseAuthenticatedCommand<bool>
{
    public Guid NotificationId { get; set; }
    
    public override bool RequiresDriver => true;
    public override bool RequiresDispatcher => false;
    public override bool RequiresAdmin => false;
    public override bool RequiresSuperAdmin => false;
    public override bool RequiresWorkspaceAccess => false;
}

public class MarkNotificationAsReadCommandHandler : BaseAuthenticatedCommandHandler<MarkNotificationAsReadCommand, bool>
{
    private readonly AppDbContext _context;

    public MarkNotificationAsReadCommandHandler(IUserService userService, AppDbContext context) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<bool> HandleCommand(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = User.Id;
        
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == userId, cancellationToken);

        if (notification == null)
            return false;

        notification.MarkAsRead();
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}