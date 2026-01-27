using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Responses.Workspace;

namespace Monolith.WebAPI.Applications.Queries.Workspace;

public class GetDepotQuery : BaseAuthenticatedCommand<DepotResponse>
{
    public int Id { get; set; }
}

public class GetDepotQueryValidator : AbstractValidator<GetDepotQuery>
{
    public GetDepotQueryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Geçerli bir depo ID'si giriniz");
    }
}

public class GetDepotQueryHandler : BaseAuthenticatedCommandHandler<GetDepotQuery, DepotResponse>
{
    private readonly AppDbContext _context;
    
    public GetDepotQueryHandler(AppDbContext context, IUserService userService) 
        : base(userService)
    {
        _context = context;
    }
    
    protected override async Task<DepotResponse> HandleCommand(GetDepotQuery request, CancellationToken cancellationToken)
    {
        var depot = await _context.Depots
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.WorkspaceId == User.WorkspaceId, cancellationToken);
            
        if (depot == null)
            throw new ApiException("Depo bulunamadı", 404);
        
        return new DepotResponse(depot);
    }
}