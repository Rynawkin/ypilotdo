// src/Monolith.WebAPI/Applications/Queries/Vehicles/GetVehiclesQuery.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Vehicles;

public class GetVehiclesQuery : BaseAuthenticatedCommand<IEnumerable<VehicleResponse>>
{
    public string? Status { get; set; }
    public string? SearchQuery { get; set; }
    
    public override bool RequiresDriver => false;
}

public class GetVehiclesQueryHandler : BaseAuthenticatedCommandHandler<GetVehiclesQuery, IEnumerable<VehicleResponse>>
{
    private readonly AppDbContext _context;

    public GetVehiclesQueryHandler(AppDbContext context, IUserService userService) 
        : base(userService)
    {
        _context = context;
    }

    protected override async Task<IEnumerable<VehicleResponse>> HandleCommand(GetVehiclesQuery request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"GetVehiclesQuery - WorkspaceId: {User.WorkspaceId}, UserId: {User.Id}, IsDriver: {User.IsDriver}");
        
        var query = _context.Vehicles
            .Where(v => v.WorkspaceId == User.WorkspaceId)
            .Where(v => v.DeletedAt == null);

        // Driver ise sadece kendisine atanan aracı görebilir
        if (User.IsDriver && !User.IsDispatcher && !User.IsAdmin && !User.IsSuperAdmin)
        {
            if (User.AssignedVehicleId.HasValue)
            {
                query = query.Where(v => v.Id == User.AssignedVehicleId.Value);
            }
            else
            {
                return new List<VehicleResponse>();
            }
        }

        // Status filtrelemesi
        if (!string.IsNullOrEmpty(request.Status))
        {
            query = query.Where(v => v.Status == request.Status);
        }

        // Arama filtrelemesi
        if (!string.IsNullOrEmpty(request.SearchQuery))
        {
            var searchLower = request.SearchQuery.ToLower();
            query = query.Where(v => 
                v.PlateNumber.ToLower().Contains(searchLower) ||
                v.Brand.ToLower().Contains(searchLower) ||
                v.Model.ToLower().Contains(searchLower));
        }

        var vehicles = await query
            .OrderBy(v => v.PlateNumber)
            .Select(v => new VehicleResponse
            {
                Id = v.Id,
                PlateNumber = v.PlateNumber,
                Type = v.Type,
                Brand = v.Brand,
                Model = v.Model,
                Year = v.Year,
                Capacity = v.Capacity,
                Status = v.Status,
                FuelType = v.FuelType,
                CurrentKm = v.CurrentKm, // ✅ YENİ
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        Console.WriteLine($"Found {vehicles.Count} vehicles for user {User.Email}");
        
        return vehicles;
    }
}