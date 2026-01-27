using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Applications.Commands.Members;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Responses.Members;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Members;

public class GetMembersQuery : BaseAuthenticatedCommand<IEnumerable<UserResponse>>
{
    public bool? IsDriver { get; set; }
    public bool? IsDispatcher { get; set; }
    public bool? IsAdmin { get; set; }
    public string? SearchQuery { get; set; }
    
    // ✅ Sadece Admin ve üstü roller üye listesini görebilir
    public override bool RequiresAdmin => true;
}

public class GetMembersQueryHandler : BaseAuthenticatedCommandHandler<GetMembersQuery, IEnumerable<UserResponse>>
{
    private readonly AppDbContext _context;

    public GetMembersQueryHandler(AppDbContext context, IUserService userService) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<IEnumerable<UserResponse>> HandleCommand(GetMembersQuery request, CancellationToken cancellationToken)
    {
        // ✅ Sadece kendi workspace'indeki üyeleri görebilir
        var usersQuery = _context.Users
            .Include(x => x.Depot)
            .Include(x => x.Workspace)
            .Where(x => x.WorkspaceId == User.WorkspaceId)
            .AsNoTracking();

        var tempUsersQuery = _context.TempMembers
            .Where(x => x.WorkspaceId == User.WorkspaceId && !x.IsSaved)
            .AsNoTracking();

        // Rol filtreleri
        if (request.IsDriver.HasValue)
        {
            usersQuery = usersQuery.Where(x => x.IsDriver == request.IsDriver.Value);
            tempUsersQuery = tempUsersQuery.Where(x => x.Roles.Contains((int)MemberRole.Driver));
        }
        
        if (request.IsDispatcher.HasValue)
        {
            usersQuery = usersQuery.Where(x => x.IsDispatcher == request.IsDispatcher.Value);
            tempUsersQuery = tempUsersQuery.Where(x => x.Roles.Contains((int)MemberRole.Dispatcher));
        }
        
        if (request.IsAdmin.HasValue)
        {
            usersQuery = usersQuery.Where(x => x.IsAdmin == request.IsAdmin.Value);
            tempUsersQuery = tempUsersQuery.Where(x => x.Roles.Contains((int)MemberRole.Admin));
        }

        // Arama filtresi
        if (!string.IsNullOrEmpty(request.SearchQuery))
        {
            var searchLower = request.SearchQuery.ToLower();
            usersQuery = usersQuery.Where(x => 
                x.FullName.ToLower().Contains(searchLower) ||
                x.Email.ToLower().Contains(searchLower) ||
                (x.PhoneNumber != null && x.PhoneNumber.Contains(searchLower)));
                
            tempUsersQuery = tempUsersQuery.Where(x => 
                x.FullName.ToLower().Contains(searchLower) ||
                x.Email.ToLower().Contains(searchLower));
        }

        var users = await usersQuery.ToListAsync(cancellationToken);
        var tempUsers = await tempUsersQuery.ToListAsync(cancellationToken);

        users.AddRange(tempUsers.Select(x => new ApplicationUser(x)));

        return users.Select(x => new UserResponse(x));
    }
}