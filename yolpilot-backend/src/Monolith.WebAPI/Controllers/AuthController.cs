using Monolith.WebAPI.Data.Seed;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Applications.Commands.Workspace;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Monolith.WebAPI.Services.Email;
using Monolith.WebAPI.Services.Members; // TokenService için

namespace Monolith.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;
    private readonly ITokenService _tokenService; // YENİ EKLENEN

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        AppDbContext context,
        IEmailService emailService,
        ILogger<AuthController> logger,
        ITokenService tokenService) // YENİ EKLENEN
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _context = context;
        _emailService = emailService;
        _logger = logger;
        _tokenService = tokenService; // YENİ EKLENEN
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        Data.Workspace.Workspace workspace = null;
        ApplicationUser user = null;
        
        // TRANSACTION BLOĞU
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                // 1. Multi-tenant için yeni workspace oluştur
                var createWorkspaceCommand = new CreateWorkspaceCommand
                {
                    WorkspaceName = model.WorkspaceName ?? $"{model.FullName}'s Company",
                    AdminUserEmail = model.Email,
                    AdminUserFullName = model.FullName
                };

                workspace = new Data.Workspace.Workspace(createWorkspaceCommand);
                _context.Workspaces.Add(workspace);
                await _context.SaveChangesAsync();

                // 2. ApplicationUser'ı workspace constructor'ı ile oluştur
                user = new ApplicationUser(createWorkspaceCommand, workspace);
                
                // Identity gereksinimleri için email'i tekrar set et
                user.UserName = model.Email;
                user.Email = model.Email;
                user.EmailConfirmed = true;
                
                // 3. User'ı oluştur
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // 4. Admin rolünü ekle
                    await _userManager.AddToRoleAsync(user, "Admin");
                    
                    // 5. Transaction'ı commit et
                    await transaction.CommitAsync();
                }
                else
                {
                    // Hata durumunda rollback
                    await transaction.RollbackAsync();
                    return BadRequest(new { success = false, errors = result.Errors });
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = ex.Message });
            }
        } // <-- TRANSACTION BLOĞU BİTTİ

        // TRANSACTION DIŞINDA template'leri ekle
        try
        {
            _logger.LogInformation($"Attempting to add template for workspace {workspace.Id}");
            
            var testTemplate = new Data.Workspace.MessageTemplate
            {
                WorkspaceId = workspace.Id,
                TemplateType = "WelcomeEmail",
                Channel = "Email", 
                Name = "Test Template",
                Subject = "Test",
                Body = "Test body",
                IsActive = true,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _logger.LogInformation($"Template created in memory");
            
            _context.MessageTemplates.Add(testTemplate);
            _logger.LogInformation($"Template added to context");
            
            var count = await _context.SaveChangesAsync();
            _logger.LogInformation($"SaveChanges completed, affected rows: {count}");
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError($"Database error: {dbEx.Message}");
            _logger.LogError($"Inner exception: {dbEx.InnerException?.Message}");
            
            if (dbEx.InnerException?.InnerException != null)
            {
                _logger.LogError($"Inner inner exception: {dbEx.InnerException.InnerException.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"General error: {ex.Message}");
            _logger.LogError($"Stack trace: {ex.StackTrace}");
        }

        // 6. Welcome email gönder
        try
        {
            await _emailService.SendWelcomeEmailToWorkspaceAdmin(
                user.Email, 
                user.FullName, 
                workspace.Name
            );
            _logger.LogInformation($"Welcome email sent to {user.Email}");
        }
        catch (Exception emailEx)
        {
            _logger.LogError(emailEx, "Welcome email gönderilemedi: {Email}", user.Email);
        }

        // 7. TOKEN'I TokenService İLE OLUŞTUR - DÜZELTILDI
        var tokenModel = _tokenService.Create(user);
        
        return Ok(new
        {
            success = true,
            message = "Registration successful",
            token = tokenModel.BearerToken, // DÜZELTILDI
            user = new
            {
                id = user.Id,
                email = user.Email,
                fullName = user.FullName,
                workspaceId = workspace.Id,
                workspaceName = workspace.Name,
                isAdmin = true
            }
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Email ile user'ı bul
        var user = await _context.Users
            .Include(u => u.Workspace)
            .FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null)
            return Unauthorized(new { success = false, message = "Invalid email or password" });

        // Password kontrolü
        var result = await _signInManager.PasswordSignInAsync(
            model.Email, 
            model.Password, 
            model.RememberMe, 
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            // TOKEN'I TokenService İLE OLUŞTUR - DÜZELTILDI
            var tokenModel = _tokenService.Create(user);

            return Ok(new
            {
                success = true,
                message = "Login successful",
                token = tokenModel.BearerToken, // DÜZELTILDI
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    fullName = user.FullName,
                    workspaceId = user.WorkspaceId,
                    workspaceName = user.Workspace?.Name,
                    isAdmin = user.IsAdmin,
                    isDispatcher = user.IsDispatcher,
                    isDriver = user.IsDriver
                }
            });
        }

        if (result.IsLockedOut)
            return Unauthorized(new { success = false, message = "Account locked out" });
        
        return Unauthorized(new { success = false, message = "Invalid email or password" });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { success = true, message = "Logged out successfully" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _context.Users
            .Include(u => u.Workspace)
            .Include(u => u.AssignedVehicle)
            .FirstOrDefaultAsync(u => u.Id == Guid.Parse(userId));
        
        if (user == null)
            return NotFound();

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            fullName = user.FullName,
            workspaceId = user.WorkspaceId,
            workspaceName = user.Workspace?.Name,
            isAdmin = user.IsAdmin,
            isDispatcher = user.IsDispatcher,
            isDriver = user.IsDriver,
            driverStatus = user.DriverStatus,
            assignedVehicleId = user.AssignedVehicleId,
            assignedVehicle = user.AssignedVehicle != null ? new
            {
                id = user.AssignedVehicle.Id,
                plateNumber = user.AssignedVehicle.PlateNumber,
                brand = user.AssignedVehicle.Brand,
                model = user.AssignedVehicle.Model
            } : null
        });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenModel model)
    {
        // Token refresh logic - ileride implemente edilecek
        return Ok(new { message = "Token refresh not implemented yet" });
    }

    // GenerateJwtToken METODU KALDIRILDI - ARTIK TokenService KULLANIYORUZ
}

// Request Models
public class RegisterModel
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FullName { get; set; }
    public string? WorkspaceName { get; set; }
}

public class LoginModel
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public bool RememberMe { get; set; }
}

public class RefreshTokenModel
{
    public required string Token { get; set; }
    public required string RefreshToken { get; set; }
}