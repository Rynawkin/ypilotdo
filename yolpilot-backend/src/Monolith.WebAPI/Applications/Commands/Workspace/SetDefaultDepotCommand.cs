using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Responses.Workspace;
using System.Text.Json.Serialization;

namespace Monolith.WebAPI.Applications.Commands.Workspace;

public class SetDefaultDepotCommand : BaseAuthenticatedCommand<DepotResponse>
{
    public override bool RequiresAdmin => true;
    public int Id { get; set; }
}

public class SetDefaultDepotCommandValidator : AbstractValidator<SetDefaultDepotCommand>
{
    public SetDefaultDepotCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Geçerli bir depo ID'si giriniz");
    }
}

public class SetDefaultDepotCommandHandler : BaseAuthenticatedCommandHandler<SetDefaultDepotCommand, DepotResponse>
{
    private readonly AppDbContext _context;
    
    public SetDefaultDepotCommandHandler(AppDbContext context, IUserService userService) 
        : base(userService)
    {
        _context = context;
    }
    
    protected override async Task<DepotResponse> HandleCommand(SetDefaultDepotCommand request, CancellationToken cancellationToken)
    {
        // Depoyu bul
        var depot = await _context.Depots
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.WorkspaceId == User.WorkspaceId, cancellationToken);
            
        if (depot == null)
            throw new ApiException("Depo bulunamadı", 404);
        
        // Zaten default ise bir şey yapma
        if (depot.IsDefault)
            return new DepotResponse(depot);
        
        // Diğer tüm depoların isDefault'unu false yap
        var existingDefaultDepots = await _context.Depots
            .Where(d => d.WorkspaceId == User.WorkspaceId && d.IsDefault)
            .ToListAsync(cancellationToken);
            
        foreach (var existingDepot in existingDefaultDepots)
        {
            existingDepot.SetAsDefault(false);
        }
        
        // Bu depoyu default yap
        depot.SetAsDefault(true);
        
        await _context.SaveChangesAsync(cancellationToken);
        
        return new DepotResponse(depot);
    }
}