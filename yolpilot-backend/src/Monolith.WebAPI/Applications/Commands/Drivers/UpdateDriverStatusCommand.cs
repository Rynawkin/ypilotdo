// src/Monolith.WebAPI/Applications/Commands/Drivers/UpdateDriverStatusCommand.cs

using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Drivers;

public class UpdateDriverStatusCommand : BaseAuthenticatedCommand<DriverResponse>
{
    public override bool RequiresAdmin => true;
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class UpdateDriverStatusCommandHandler : BaseAuthenticatedCommandHandler<UpdateDriverStatusCommand, DriverResponse>
{
    private readonly AppDbContext _context;

    public UpdateDriverStatusCommandHandler(AppDbContext context, IUserService userService) : base(userService)
    {
        _context = context;
    }

    protected override async Task<DriverResponse> HandleCommand(UpdateDriverStatusCommand request, CancellationToken cancellationToken)
    {
        var driver = await _context.Set<Data.Workspace.Driver>()
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.WorkspaceId == User.WorkspaceId, cancellationToken);

        if (driver == null)
        {
            throw new KeyNotFoundException($"Sürücü bulunamadı: {request.Id}");
        }

        // Validate status
        var validStatuses = new[] { "available", "busy", "offline" };
        if (!validStatuses.Contains(request.Status))
        {
            throw new ArgumentException($"Geçersiz durum: {request.Status}. Geçerli durumlar: {string.Join(", ", validStatuses)}");
        }

        driver.Status = request.Status;
        driver.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync(cancellationToken);

        return driver.ToResponse();
    }
}