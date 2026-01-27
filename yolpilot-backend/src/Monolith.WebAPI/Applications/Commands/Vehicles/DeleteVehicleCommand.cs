using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Vehicles;

public class DeleteVehicleCommand : BaseAuthenticatedCommand<bool>
{
    [JsonIgnore] public override bool RequiresAdmin => true;  // SECURITY: Only admins can delete vehicles

    public int VehicleId { get; set; }
}

public class DeleteVehicleCommandValidator : AbstractValidator<DeleteVehicleCommand>
{
    public DeleteVehicleCommandValidator()
    {
        RuleFor(x => x.VehicleId)
            .GreaterThan(0).WithMessage("Invalid vehicle ID");
    }
}

public class DeleteVehicleCommandHandler : BaseAuthenticatedCommandHandler<DeleteVehicleCommand, bool>
{
    private readonly AppDbContext _dbContext;

    public DeleteVehicleCommandHandler(IUserService userService, AppDbContext dbContext) : base(userService)
    {
        _dbContext = dbContext;
    }

    protected override async Task<bool> HandleCommand(DeleteVehicleCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.WorkspaceId == User.WorkspaceId, cancellationToken);

        if (vehicle == null)
            throw new Exception("Vehicle not found");

        _dbContext.Vehicles.Remove(vehicle);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}