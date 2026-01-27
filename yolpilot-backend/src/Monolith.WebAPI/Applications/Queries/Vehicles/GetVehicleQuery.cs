// src/Monolith.WebAPI/Applications/Queries/Vehicles/GetVehicleQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Vehicles;

public class GetVehicleQuery : BaseAuthenticatedCommand<VehicleResponse>
{
    public int VehicleId { get; set; }
    
    // Herkes erişebilir ama kontrol var
    public override bool RequiresDriver => false;
}

public class GetVehicleQueryHandler : BaseAuthenticatedCommandHandler<GetVehicleQuery, VehicleResponse>
{
    private readonly AppDbContext _context;

    public GetVehicleQueryHandler(AppDbContext context, IUserService userService) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<VehicleResponse> HandleCommand(GetVehicleQuery request, CancellationToken cancellationToken)
    {
        var vehicle = await _context.Vehicles
            .Where(v => v.Id == request.VehicleId && v.WorkspaceId == User.WorkspaceId)
            .Where(v => v.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (vehicle == null)
            throw new ApiException("Vehicle not found.", 404);

        // Driver ise sadece kendisine atanan aracı görebilir
        if (User.IsDriver && !User.IsDispatcher && !User.IsAdmin && !User.IsSuperAdmin)
        {
            if (!User.AssignedVehicleId.HasValue || User.AssignedVehicleId.Value != vehicle.Id)
            {
                throw new ApiException("You are not authorized to view this vehicle.", 403);
            }
        }

        return new VehicleResponse
        {
            Id = vehicle.Id,
            PlateNumber = vehicle.PlateNumber,
            Type = vehicle.Type,
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            Year = vehicle.Year,
            Capacity = vehicle.Capacity,
            Status = vehicle.Status,
            FuelType = vehicle.FuelType,
            CurrentKm = vehicle.CurrentKm, // ✅ YENİ
            CreatedAt = vehicle.CreatedAt,
            UpdatedAt = vehicle.UpdatedAt
        };
    }
}