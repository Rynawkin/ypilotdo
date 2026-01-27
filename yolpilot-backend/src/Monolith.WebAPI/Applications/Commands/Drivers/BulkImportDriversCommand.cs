// src/Monolith.WebAPI/Applications/Commands/Drivers/BulkImportDriversCommand.cs

using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Drivers;

public class BulkImportDriversCommand : BaseAuthenticatedCommand<BulkImportResult>
{
    public override bool RequiresAdmin => true;
    public List<BulkImportDriverDto> Drivers { get; set; } = new();
}

public class BulkImportResult
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<DriverResponse> ImportedDrivers { get; set; } = new();
}

public class BulkImportDriversCommandHandler : BaseAuthenticatedCommandHandler<BulkImportDriversCommand, BulkImportResult>
{
    private readonly AppDbContext _context;

    public BulkImportDriversCommandHandler(AppDbContext context, IUserService userService) : base(userService)
    {
        _context = context;
    }

    protected override async Task<BulkImportResult> HandleCommand(BulkImportDriversCommand request, CancellationToken cancellationToken)
    {
        var result = new BulkImportResult();
        
        // Get existing license numbers to check for duplicates
        var existingLicenseNumbers = await _context.Set<Data.Workspace.Driver>()
            .Where(d => d.WorkspaceId == User.WorkspaceId)
            .Select(d => d.LicenseNumber)
            .ToListAsync(cancellationToken);

        var driversToAdd = new List<Data.Workspace.Driver>();

        foreach (var driverDto in request.Drivers)
        {
            try
            {
                // Check for duplicate license number
                if (existingLicenseNumbers.Contains(driverDto.LicenseNumber))
                {
                    result.FailureCount++;
                    result.Errors.Add($"Ehliyet numarası zaten mevcut: {driverDto.LicenseNumber} ({driverDto.Name})");
                    continue;
                }

                var driver = driverDto.ToBulkImportEntity(User.WorkspaceId);
                driversToAdd.Add(driver);
                existingLicenseNumbers.Add(driver.LicenseNumber); // Add to check list for batch duplicates
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.Errors.Add($"Hata ({driverDto.Name}): {ex.Message}");
            }
        }

        if (driversToAdd.Any())
        {
            _context.Set<Data.Workspace.Driver>().AddRange(driversToAdd);
            await _context.SaveChangesAsync(cancellationToken);
            
            result.SuccessCount = driversToAdd.Count;
            result.ImportedDrivers = driversToAdd.Select(d => d.ToResponse()).ToList();
        }

        return result;
    }
}