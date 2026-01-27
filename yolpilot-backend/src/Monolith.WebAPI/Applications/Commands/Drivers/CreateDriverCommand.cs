using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Services.Email;
using Monolith.WebAPI.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Monolith.WebAPI.Applications.Commands.Drivers;

public class CreateDriverCommand : BaseAuthenticatedCommand<DriverResponse>
{
    public override bool RequiresAdmin => true;
    public CreateDriverDto Data { get; set; } = new();
}

public class CreateDriverCommandHandler : BaseAuthenticatedCommandHandler<CreateDriverCommand, DriverResponse>
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<CreateDriverCommandHandler> _logger;

    public CreateDriverCommandHandler(
        AppDbContext context, 
        IUserService userService,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<CreateDriverCommandHandler> logger) : base(userService)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    protected override async Task<DriverResponse> HandleCommand(CreateDriverCommand request, CancellationToken cancellationToken)
    {
        // Email zorunlu kontrolü
        if (string.IsNullOrWhiteSpace(request.Data.Email))
        {
            throw new ApiException("Sürücü için email adresi zorunludur", 400);
        }

        // Password kontrolü - Eğer şifre girilmemişse otomatik oluştur
        string password = request.Data.Password;
        bool isPasswordGenerated = false;
        
        if (string.IsNullOrWhiteSpace(password))
        {
            // Otomatik şifre oluştur
            password = GenerateRandomPassword();
            isPasswordGenerated = true;
        }

        // Check if email already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Data.Email);
        if (existingUser != null)
        {
            throw new ApiException($"Bu email adresi zaten kullanımda: {request.Data.Email}", 400);
        }

        // Check if license number already exists in workspace
        var existingDriver = await _context.Set<Data.Workspace.Driver>()
            .FirstOrDefaultAsync(d => d.WorkspaceId == User.WorkspaceId && 
                                     d.LicenseNumber == request.Data.LicenseNumber, 
                                     cancellationToken);

        if (existingDriver != null)
        {
            throw new ApiException($"Bu ehliyet numarasına sahip bir sürücü zaten mevcut: {request.Data.LicenseNumber}", 400);
        }

        // Check if vehicle is already assigned to another driver
        if (request.Data.VehicleId.HasValue)
        {
            var vehicleAssigned = await _context.Set<Data.Workspace.Driver>()
                .AnyAsync(d => d.WorkspaceId == User.WorkspaceId && 
                              d.VehicleId == request.Data.VehicleId && 
                              d.Status != "offline", 
                              cancellationToken);

            if (vehicleAssigned)
            {
                throw new ApiException("Bu araç başka bir sürücüye atanmış durumda", 400);
            }
        }

        // Transaction başlat
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // 1. ApplicationUser oluştur
            var applicationUser = new ApplicationUser
            {
                UserName = request.Data.Email,
                Email = request.Data.Email,
                EmailConfirmed = true, // Email doğrulamasını şimdilik atlıyoruz
                PhoneNumber = request.Data.Phone,
                PhoneNumberConfirmed = false
            };

            // Private setter'ları reflection ile set et
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.FullName))
                ?.SetValue(applicationUser, request.Data.Name);
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.WorkspaceId))
                ?.SetValue(applicationUser, User.WorkspaceId);
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.IsDriver))
                ?.SetValue(applicationUser, true);
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.IsAdmin))
                ?.SetValue(applicationUser, false);
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.IsDispatcher))
                ?.SetValue(applicationUser, false);
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.IsSuperAdmin))
                ?.SetValue(applicationUser, false);
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.IsOnboarded))
                ?.SetValue(applicationUser, true);
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.LicenseNumber))
                ?.SetValue(applicationUser, request.Data.LicenseNumber);
            typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.DriverStatus))
                ?.SetValue(applicationUser, request.Data.Status);
            
            // Depot ayarla (default depot varsa)
            var defaultDepot = await _context.Depots
                .FirstOrDefaultAsync(d => d.WorkspaceId == User.WorkspaceId && d.IsDefault, cancellationToken);
            if (defaultDepot != null)
            {
                typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.DepotId))
                    ?.SetValue(applicationUser, defaultDepot.Id);
            }

            // UserManager ile kullanıcıyı oluştur (şifre hash'leme dahil)
            var createUserResult = await _userManager.CreateAsync(applicationUser, password);
            
            if (!createUserResult.Succeeded)
            {
                var errors = string.Join(", ", createUserResult.Errors.Select(e => e.Description));
                throw new ApiException($"Kullanıcı oluşturulamadı: {errors}", 400);
            }

            // 2. Driver entity'si oluştur
            var driver = request.Data.ToEntity(User.WorkspaceId);
            driver.UserId = applicationUser.Id; // UserId'yi set et
            
            _context.Set<Data.Workspace.Driver>().Add(driver);
            await _context.SaveChangesAsync(cancellationToken);

            // 3. Email gönder
            try 
            {
                await _emailService.SendWelcomeEmailToDriver(
                    applicationUser.Email, 
                    request.Data.Name,
                    password
                );
                _logger.LogInformation($"Welcome email sent to driver: {applicationUser.Email}");
            }
            catch (Exception ex)
            {
                // Email gönderilemese de driver oluşturulmuş olsun
                _logger.LogError(ex, $"Welcome email could not be sent to driver: {applicationUser.Email}");
            }

            // Transaction'ı commit et
            await transaction.CommitAsync(cancellationToken);

            // Response döndür
            var response = driver.ToResponse();
            
            // Email ve login bilgilerini response'a ekle (frontend'de göstermek için)
            response.LoginEmail = applicationUser.Email;
            response.IsUserCreated = true;
            
            // Eğer şifre otomatik oluşturulduysa, frontend'e bildir (opsiyonel)
            if (isPasswordGenerated)
            {
                response.PasswordGenerated = true;
                response.GeneratedPassword = password; // Güvenlik için bunu production'da göstermeyin
            }
            
            return response;
        }
        catch
        {
            // Hata durumunda rollback
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
        var random = new Random();
        var password = new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        
        // En az bir büyük harf, bir küçük harf, bir rakam ve bir özel karakter olsun
        if (!password.Any(char.IsUpper))
            password = "A" + password.Substring(1);
        if (!password.Any(char.IsLower))
            password = password.Substring(0, 1) + "a" + password.Substring(2);
        if (!password.Any(char.IsDigit))
            password = password.Substring(0, 2) + "2" + password.Substring(3);
        if (!password.Any(c => "!@#$%".Contains(c)))
            password = password.Substring(0, 3) + "!" + password.Substring(4);
            
        return password;
    }
}