using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;
using System.Text.Json.Serialization;

namespace Monolith.WebAPI.Applications.Commands.Workspace;

public class DeleteDepotCommand : BaseAuthenticatedCommand<bool>
{
    public override bool RequiresAdmin => true;  // SECURITY: Only admins can delete depots
    public int Id { get; set; }
}

public class DeleteDepotCommandValidator : AbstractValidator<DeleteDepotCommand>
{
    public DeleteDepotCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Geçerli bir depo ID'si giriniz");
    }
}

public class DeleteDepotCommandHandler : BaseAuthenticatedCommandHandler<DeleteDepotCommand, bool>
{
    private readonly AppDbContext _context;
    
    public DeleteDepotCommandHandler(AppDbContext context, IUserService userService) 
        : base(userService)
    {
        _context = context;
    }
    
    protected override async Task<bool> HandleCommand(DeleteDepotCommand request, CancellationToken cancellationToken)
    {
        // Depoyu bul
        var depot = await _context.Depots
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.WorkspaceId == User.WorkspaceId, cancellationToken);
            
        if (depot == null)
            throw new ApiException("Depo bulunamadı", 404);
        
        // Ana depo silinemez
        if (depot.IsDefault)
            throw new ApiException("Ana depo silinemez. Önce başka bir depoyu ana depo olarak ayarlayın.", 400);
        
        // TODO: Route'larda kullanılıyor mu kontrol et
        // var routesUsingDepot = await _context.Routes
        //     .AnyAsync(r => r.DepotId == request.Id, cancellationToken);
        // 
        // if (routesUsingDepot)
        //     throw new ApiException("Bu depo aktif rotalarda kullanılıyor ve silinemez", 400);
        
        _context.Depots.Remove(depot);
        await _context.SaveChangesAsync(cancellationToken);
        
        return true;
    }
}