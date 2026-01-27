using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Vehicles;

public class UpdateVehicleStatusCommand : BaseAuthenticatedCommand<VehicleResponse>
{
    [JsonIgnore] public override bool RequiresDispatcher => true;
    
    public int VehicleId { get; set; }
    public string Status { get; set; }
}

public class UpdateVehicleStatusCommandValidator : AbstractValidator<UpdateVehicleStatusCommand>
{
    public UpdateVehicleStatusCommandValidator()
    {
        RuleFor(x => x.VehicleId)
            .GreaterThan(0).WithMessage("Invalid vehicle ID");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(x => new[] { "active", "inactive", "maintenance" }.Contains(x.ToLower()))
            .WithMessage("Status must be active, inactive or maintenance");
    }
}

public class UpdateVehicleStatusCommandHandler : BaseAuthenticatedCommandHandler<UpdateVehicleStatusCommand, VehicleResponse>
{
    private readonly AppDbContext _dbContext;

    public UpdateVehicleStatusCommandHandler(IUserService userService, AppDbContext dbContext) : base(userService)
    {
        _dbContext = dbContext;
    }

    protected override async Task<VehicleResponse> HandleCommand(UpdateVehicleStatusCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.WorkspaceId == User.WorkspaceId, cancellationToken);

        if (vehicle == null)
            throw new Exception("Vehicle not found");

        vehicle.Status = request.Status.ToLower();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new VehicleResponse
        {
            Id = vehicle.Id,
            PlateNumber = vehicle.PlateNumber,
            Type = vehicle.Type,
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            Year = vehicle.Year,
            Capacity = vehicle.Capacity,
            FuelType = vehicle.FuelType,
            Status = vehicle.Status,
            CurrentKm = vehicle.CurrentKm // ✅ YENİ
        };
    }
}