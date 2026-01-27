using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Vehicles;

public class CreateVehicleCommand : BaseAuthenticatedCommand<VehicleResponse>
{
    [JsonIgnore] public override bool RequiresDispatcher => true;
    
    public string PlateNumber { get; set; }
    public string Type { get; set; }
    public string Brand { get; set; }
    public string Model { get; set; }
    public int Year { get; set; }
    public int Capacity { get; set; }
    public string FuelType { get; set; }
    public string Status { get; set; }
    public int? CurrentKm { get; set; } // ✅ YENİ - Başlangıç kilometresi (opsiyonel)
}

public class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleCommandValidator()
    {
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

        // ✅ YENİ - CurrentKm validation (opsiyonel ama >= 0 olmalı)
        RuleFor(x => x.CurrentKm)
            .GreaterThanOrEqualTo(0)
            .When(x => x.CurrentKm.HasValue)
            .WithMessage("Kilometre bilgisi 0'dan küçük olamaz");
    }
}

public class CreateVehicleCommandHandler : BaseAuthenticatedCommandHandler<CreateVehicleCommand, VehicleResponse>
{
    private readonly AppDbContext _dbContext;

    public CreateVehicleCommandHandler(IUserService userService, AppDbContext dbContext) : base(userService)
    {
        _dbContext = dbContext;
    }

    protected override async Task<VehicleResponse> HandleCommand(CreateVehicleCommand request, CancellationToken cancellationToken)
    {
        var workspace = await _dbContext.Workspaces
            .Include(w => w.Vehicles)
            .FirstOrDefaultAsync(w => w.Id == User.WorkspaceId, cancellationToken);

        if (workspace == null)
            throw new Exception("Workspace not found");

        var vehicle = new Vehicle
        {
            PlateNumber = request.PlateNumber,
            Type = request.Type,
            Brand = request.Brand,
            Model = request.Model,
            Year = request.Year,
            Capacity = request.Capacity,
            FuelType = request.FuelType,
            Status = request.Status.ToLower(),
            CurrentKm = request.CurrentKm, // ✅ YENİ - Başlangıç kilometresi
            WorkspaceId = User.WorkspaceId
        };

        workspace.Vehicles.Add(vehicle);
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