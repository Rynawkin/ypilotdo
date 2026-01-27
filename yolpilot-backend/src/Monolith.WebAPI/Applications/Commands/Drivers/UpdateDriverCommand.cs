// src/Monolith.WebAPI/Applications/Commands/Drivers/UpdateDriverCommand.cs

using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Drivers;

public class UpdateDriverCommand : BaseAuthenticatedCommand<DriverResponse>
{
    public override bool RequiresAdmin => true;
    public int Id { get; set; }
    public UpdateDriverDto Data { get; set; } = new();
}

public class UpdateDriverCommandHandler : BaseAuthenticatedCommandHandler<UpdateDriverCommand, DriverResponse>
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UpdateDriverCommandHandler(
        AppDbContext context, 
        IUserService userService,
        UserManager<ApplicationUser> userManager) : base(userService)
    {
        _context = context;
        _userManager = userManager;
    }

    protected override async Task<DriverResponse> HandleCommand(UpdateDriverCommand request, CancellationToken cancellationToken)
    {
        var driver = await _context.Set<Data.Workspace.Driver>()
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.WorkspaceId == User.WorkspaceId, cancellationToken);

        if (driver == null)
        {
            throw new KeyNotFoundException($"Sürücü bulunamadı: {request.Id}");
        }

        // Check if license number is being changed and already exists
        if (driver.LicenseNumber != request.Data.LicenseNumber)
        {
            var existingDriver = await _context.Set<Data.Workspace.Driver>()
                .FirstOrDefaultAsync(d => d.WorkspaceId == User.WorkspaceId && 
                                         d.LicenseNumber == request.Data.LicenseNumber && 
                                         d.Id != request.Id, 
                                         cancellationToken);

            if (existingDriver != null)
            {
                throw new InvalidOperationException($"Bu ehliyet numarasına sahip başka bir sürücü mevcut: {request.Data.LicenseNumber}");
            }
        }

        // Check if vehicle is being changed and already assigned
        if (request.Data.VehicleId.HasValue && request.Data.VehicleId != driver.VehicleId)
        {
            var vehicleAssigned = await _context.Set<Data.Workspace.Driver>()
                .AnyAsync(d => d.WorkspaceId == User.WorkspaceId && 
                              d.VehicleId == request.Data.VehicleId && 
                              d.Id != request.Id &&
                              d.Status != "offline", 
                              cancellationToken);

            if (vehicleAssigned)
            {
                throw new InvalidOperationException("Bu araç başka bir sürücüye atanmış durumda");
            }
        }

        // Transaction başlat
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // Driver entity'sini güncelle
            driver.UpdateEntity(request.Data);
            
            // Eğer UserId varsa, ApplicationUser'ı da güncelle
            if (driver.UserId.HasValue)
            {
                var applicationUser = await _userManager.FindByIdAsync(driver.UserId.Value.ToString());
                if (applicationUser != null)
                {
                    // Temel bilgileri güncelle
                    typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.FullName))
                        ?.SetValue(applicationUser, request.Data.Name);
                    typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.LicenseNumber))
                        ?.SetValue(applicationUser, request.Data.LicenseNumber);
                    typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.DriverStatus))
                        ?.SetValue(applicationUser, request.Data.Status);
                    
                    applicationUser.PhoneNumber = request.Data.Phone;
                    
                    // Email değişikliği varsa
                    if (!string.IsNullOrWhiteSpace(request.Data.Email) && request.Data.Email != applicationUser.Email)
                    {
                        // Email benzersizlik kontrolü
                        var existingEmailUser = await _userManager.FindByEmailAsync(request.Data.Email);
                        if (existingEmailUser != null && existingEmailUser.Id != applicationUser.Id)
                        {
                            throw new ApiException($"Bu email adresi zaten kullanımda: {request.Data.Email}", 400);
                        }
                        
                        applicationUser.Email = request.Data.Email;
                        applicationUser.UserName = request.Data.Email;
                        applicationUser.NormalizedEmail = request.Data.Email.ToUpper();
                        applicationUser.NormalizedUserName = request.Data.Email.ToUpper();
                    }
                    
                    // Kullanıcıyı güncelle
                    var updateResult = await _userManager.UpdateAsync(applicationUser);
                    if (!updateResult.Succeeded)
                    {
                        var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                        throw new ApiException($"Kullanıcı güncellenemedi: {errors}", 400);
                    }
                    
                    // Şifre değişikliği varsa
                    if (!string.IsNullOrWhiteSpace(request.Data.NewPassword))
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(applicationUser);
                        var passwordResult = await _userManager.ResetPasswordAsync(applicationUser, token, request.Data.NewPassword);
                        
                        if (!passwordResult.Succeeded)
                        {
                            var errors = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
                            throw new ApiException($"Şifre güncellenemedi: {errors}", 400);
                        }
                    }
                }
            }
            
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            return driver.ToResponse();
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}