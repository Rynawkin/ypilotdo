// src/Monolith.WebAPI/Applications/Commands/Members/UpdateMemberCommand.cs

using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Members;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Members;

public class UpdateMemberCommand : BaseAuthenticatedCommand<UserResponse>
{
    [JsonIgnore] public override bool RequiresAdmin => true;
    [JsonIgnore] public Guid UserId { get; set; }
    
    public int? DepotId { get; set; }
    public int[]? Roles { get; set; }
}

public class UpdateMemberCommandValidator : AbstractValidator<UpdateMemberCommand>
{
    public UpdateMemberCommandValidator()
    {
        RuleFor(x => x.DepotId).GreaterThan(0).When(x => x.DepotId.HasValue);
    }
}

public class UpdateMemberCommandHandler : BaseAuthenticatedCommandHandler<UpdateMemberCommand, UserResponse>
{
    private readonly AppDbContext _context;

    public UpdateMemberCommandHandler(AppDbContext context, IUserService userService) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<UserResponse> HandleCommand(UpdateMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await _context.Users
            .Include(u => u.Depot)
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.WorkspaceId == User.WorkspaceId, cancellationToken);
        
        if (member == null)
            throw new ApiException("Member not found", 404);
        
        // Depot güncelleme
        if (request.DepotId.HasValue)
        {
            var depot = await _context.Depots
                .FirstOrDefaultAsync(d => d.Id == request.DepotId.Value && d.WorkspaceId == User.WorkspaceId, cancellationToken);
            
            if (depot == null)
                throw new ApiException("Depot not found", 404);
            
            member.SetDepotId(request.DepotId.Value);
        }
        
        // Rol güncelleme
        if (request.Roles != null)
        {
            member.SetDispatcher(request.Roles.Contains((int)MemberRole.Dispatcher));
            member.SetDriver(request.Roles.Contains((int)MemberRole.Driver));
            // Admin rolü sadece SuperAdmin değiştirebilir
            if (User.IsSuperAdmin)
            {
                member.SetAdmin(request.Roles.Contains((int)MemberRole.Admin));
            }
        }
        
        _context.Users.Update(member);
        await _context.SaveChangesAsync(cancellationToken);
        
        return new UserResponse(member);
    }
}