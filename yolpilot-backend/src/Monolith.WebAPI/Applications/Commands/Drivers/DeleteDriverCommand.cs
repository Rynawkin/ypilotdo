// src/Monolith.WebAPI/Applications/Commands/Drivers/DeleteDriverCommand.cs

using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Drivers;

public class DeleteDriverCommand : BaseAuthenticatedCommand<bool>
{
    public override bool RequiresAdmin => true;
    public int Id { get; set; }
}

public class DeleteDriverCommandHandler : BaseAuthenticatedCommandHandler<DeleteDriverCommand, bool>
{
    private readonly AppDbContext _context;

    public DeleteDriverCommandHandler(AppDbContext context, IUserService userService) : base(userService)
    {
        _context = context;
    }

    protected override async Task<bool> HandleCommand(DeleteDriverCommand request, CancellationToken cancellationToken)
    {
        var driver = await _context.Set<Data.Workspace.Driver>()
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.WorkspaceId == User.WorkspaceId, cancellationToken);

        if (driver == null)
        {
            throw new KeyNotFoundException($"Sürücü bulunamadı: {request.Id}");
        }

        // Check if driver has active journeys
        var hasActiveJourneys = await _context.Set<Journey>()
            .AnyAsync(j => j.DriverId == request.Id && 
                          j.CompletedAt == null,
                          cancellationToken);

        if (hasActiveJourneys)
        {
            throw new InvalidOperationException("Bu sürücünün aktif rotaları bulunmaktadır. Önce rotaları tamamlayın veya iptal edin.");
        }

        _context.Set<Data.Workspace.Driver>().Remove(driver);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}