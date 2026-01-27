// src/Monolith.WebAPI/Applications/Commands/Members/DeleteMemberCommand.cs

using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Members;

public class DeleteMemberCommand : BaseAuthenticatedCommand<Unit>
{
    [JsonIgnore] public override bool RequiresAdmin => true;
    [JsonIgnore] public Guid UserId { get; set; }
}

public class DeleteMemberCommandHandler : BaseAuthenticatedCommandHandler<DeleteMemberCommand, Unit>
{
    private readonly AppDbContext _context;

    public DeleteMemberCommandHandler(AppDbContext context, IUserService userService) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<Unit> HandleCommand(DeleteMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.WorkspaceId == User.WorkspaceId, cancellationToken);
        
        if (member == null)
            throw new ApiException("Member not found", 404);
        
        // Admin kendini silemez
        if (member.Id == User.Id)
            throw new ApiException("You cannot delete yourself", 400);
        
        // Başka bir admin silinemez
        if (member.IsAdmin)
            throw new ApiException("Cannot delete another admin", 403);
        
        // HARD DELETE - direkt sil
        _context.Users.Remove(member);
        await _context.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}