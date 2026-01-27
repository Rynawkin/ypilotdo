using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Vehicles;

public class UpdateVehicleCommand : BaseAuthenticatedCommand<VehicleResponse>
{
    [JsonIgnore] public override bool RequiresDispatcher => true;

    public int VehicleId { get; set; }
    public string PlateNumber { get; set; }
    public string Type { get; set; }
    public string Brand { get; set; }
    public string Model { get; set; }
    public int Year { get; set; }
    public int Capacity { get; set; }
    public string FuelType { get; set; }
    public string Status { get; set; }
    public int? CurrentKm { get; set; } // ✅ YENİ - Araç kilometresi
}

public class UpdateVehicleCommandValidator : AbstractValidator<UpdateVehicleCommand>
{
    public UpdateVehicleCommandValidator()
    {
        RuleFor(x => x.VehicleId)
            .GreaterThan(0).WithMessage("Invalid vehicle ID");

        RuleFor(x => x.PlateNumber)
            .NotEmpty().WithMessage("Plate number is required")
            .MaximumLength(20);

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Vehicle type is required")
            .MaximumLength(50);

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Brand is required")
            .MaximumLength(50);

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Model is required")
            .MaximumLength(50);

        RuleFor(x => x.Year)
            .InclusiveBetween(1900, DateTime.Now.Year + 1)
            .WithMessage("Invalid year");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be greater than 0");

        RuleFor(x => x.FuelType)
            .NotEmpty().WithMessage("Fuel type is required")
            .MaximumLength(30);

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(x => new[] { "active", "inactive", "maintenance" }.Contains(x.ToLower()))
            .WithMessage("Status must be active, inactive or maintenance");
    }
}

public class UpdateVehicleCommandHandler : BaseAuthenticatedCommandHandler<UpdateVehicleCommand, VehicleResponse>
{
    private readonly AppDbContext _dbContext;

    public UpdateVehicleCommandHandler(IUserService userService, AppDbContext dbContext) : base(userService)
    {
        _dbContext = dbContext;
    }

    protected override async Task<VehicleResponse> HandleCommand(UpdateVehicleCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.WorkspaceId == User.WorkspaceId, cancellationToken);

        if (vehicle == null)
            throw new Exception("Vehicle not found");

        vehicle.PlateNumber = request.PlateNumber;
        vehicle.Type = request.Type;
        vehicle.Brand = request.Brand;
        vehicle.Model = request.Model;
        vehicle.Year = request.Year;
        vehicle.Capacity = request.Capacity;
        vehicle.FuelType = request.FuelType;
        vehicle.Status = request.Status.ToLower();

        // ✅ YENİ - CurrentKm güncelleme
        if (request.CurrentKm.HasValue)
        {
            vehicle.CurrentKm = request.CurrentKm.Value;
        }

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