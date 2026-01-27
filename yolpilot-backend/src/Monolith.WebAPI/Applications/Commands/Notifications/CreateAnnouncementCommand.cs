using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Notifications;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Notifications;

public class CreateAnnouncementCommand : BaseAuthenticatedCommand<Unit>
{
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? TargetRole { get; set; } // null = all users, "Driver", "Dispatcher", etc.
    public int? WorkspaceId { get; set; } // null = all workspaces
    
    public override bool RequiresDriver => false;
    public override bool RequiresDispatcher => false;
    public override bool RequiresAdmin => true;
    public override bool RequiresSuperAdmin => false;
    public override bool RequiresWorkspaceAccess => false;
}

public class CreateAnnouncementCommandHandler : BaseAuthenticatedCommandHandler<CreateAnnouncementCommand, Unit>
{
    private readonly AppDbContext _context;

    public CreateAnnouncementCommandHandler(IUserService userService, AppDbContext context) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<Unit> HandleCommand(CreateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        // Get target users
        var usersQuery = _context.Users.AsQueryable();

        // Filter by workspace if specified
        if (request.WorkspaceId.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.WorkspaceId == request.WorkspaceId.Value);
        }

        // Filter by role if specified
        if (!string.IsNullOrEmpty(request.TargetRole))
        {
            switch (request.TargetRole.ToLower())
            {
                case "driver":
                    usersQuery = usersQuery.Where(u => u.IsDriver);
                    break;
                case "dispatcher":
                    usersQuery = usersQuery.Where(u => u.IsDispatcher);
                    break;
                case "admin":
                    usersQuery = usersQuery.Where(u => u.IsAdmin);
                    break;
                case "superadmin":
                    usersQuery = usersQuery.Where(u => u.IsSuperAdmin);
                    break;
            }
        }

        var targetUsers = await usersQuery.ToListAsync(cancellationToken);

        // Create notifications for all target users
        foreach (var user in targetUsers)
        {
            var notification = new Notification(
                user.WorkspaceId,
                user.Id,
                request.Title,
                request.Message,
                NotificationType.SYSTEM_ANNOUNCEMENT
            );

            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}