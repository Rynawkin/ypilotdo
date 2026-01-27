using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Notifications;
using Monolith.WebAPI.Responses.Notifications;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Notifications;

public class GetNotificationsQuery : BaseAuthenticatedCommand<NotificationsListResponse>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool? IsRead { get; set; }
    public string? Type { get; set; }
    
    public override bool RequiresDriver => true;
    public override bool RequiresDispatcher => false;
    public override bool RequiresAdmin => false;
    public override bool RequiresSuperAdmin => false;
    public override bool RequiresWorkspaceAccess => false;
}

public class GetNotificationsQueryHandler : BaseAuthenticatedCommandHandler<GetNotificationsQuery, NotificationsListResponse>
{
    private readonly AppDbContext _context;

    public GetNotificationsQueryHandler(IUserService userService, AppDbContext context) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<NotificationsListResponse> HandleCommand(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = User.Id;
        
        // Base query
        var query = _context.Notifications
            .Where(n => n.UserId == userId);

        // Apply filters
        if (request.IsRead.HasValue)
        {
            query = query.Where(n => n.IsRead == request.IsRead.Value);
        }

        if (!string.IsNullOrEmpty(request.Type))
        {
            if (Enum.TryParse<NotificationType>(request.Type, true, out var notificationType))
            {
                query = query.Where(n => n.Type == notificationType);
            }
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new NotificationsListResponse
        {
            Notifications = notifications.Select(NotificationResponse.FromEntity).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}