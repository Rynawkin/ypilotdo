using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Seed;
using Monolith.WebAPI.Applications.Commands.Workspace;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Monolith.WebAPI.Services.Email;
using Monolith.WebAPI.Services.Members; // TokenService için

namespace Monolith.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly ILogger<MeController> _logger;
    private readonly IEmailService _emailService;
    private readonly ITokenService _tokenService; // YENİ EKLENEN

    public MeController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        AppDbContext context,
        ILogger<MeController> logger,
        IEmailService emailService,
        ITokenService tokenService) // YENİ EKLENEN
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _tokenService = tokenService; // YENİ EKLENEN
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] MeLoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Geçersiz istek formatı" });

        _logger.LogInformation("Login attempt for: " + request.Email);

        // Email ile user'ı bul
        var user = await _context.Users
            .Include(u => u.Workspace)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            _logger.LogWarning("User not found: " + request.Email);
            return Unauthorized(new { message = "Email veya şifre hatalı!" });
        }

        _logger.LogInformation("User found: " + user.Email + ", WorkspaceId: " + user.WorkspaceId);

        // Password kontrolü
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

        if (result.Succeeded)
        {
            // Workspace kontrolü
            if (user.WorkspaceId <= 0)
            {
                _logger.LogError("User " + user.Email + " has invalid WorkspaceId: " + user.WorkspaceId);
                
                // Default workspace ata
                var defaultWorkspace = await _context.Workspaces.FirstOrDefaultAsync();
                if (defaultWorkspace != null)
                {
                    typeof(ApplicationUser)
                        .GetProperty(nameof(ApplicationUser.WorkspaceId))
                        ?.SetValue(user, defaultWorkspace.Id);
                    
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Assigned default workspace " + defaultWorkspace.Id + " to user " + user.Email);
                }
            }

            // TOKEN'I TokenService İLE OLUŞTUR - DÜZELTILDI
            var tokenModel = _tokenService.Create(user);
            
            // User'ın rollerini al
            var userRoles = await _userManager.GetRolesAsync(user);
            var isSuperAdmin = userRoles.Contains("SuperAdmin");
            
            string roleLog = user.IsAdmin ? "admin" : user.IsDispatcher ? "dispatcher" : user.IsDriver ? "driver" : "user";
            _logger.LogInformation("Login successful for: " + user.Email + ", Role: " + roleLog + ", WorkspaceId: " + user.WorkspaceId);
            
            // Frontend'in beklediği format
            return Ok(new MeTokenModel
            {
                BearerToken = tokenModel.BearerToken, // DÜZELTILDI
                Me = new MeUserModel
                {
                    Id = user.Id.ToString(),
                    Email = user.Email,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    WorkspaceId = user.WorkspaceId,
                    WorkspaceName = user.Workspace?.Name,
                    IsAdmin = user.IsAdmin,
                    IsDispatcher = user.IsDispatcher,
                    IsDriver = user.IsDriver,
                    IsSuperAdmin = isSuperAdmin,
                    IsOnboarded = user.IsOnboarded,
                    DepotId = user.DepotId
                }
            });
        }

        if (result.IsLockedOut)
            return Unauthorized(new { message = "Hesabınız kilitlendi. Lütfen daha sonra tekrar deneyin." });

        return Unauthorized(new { message = "Email veya şifre hatalı!" });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] MeRegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Geçersiz istek formatı" });

        // Email kontrolü
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return BadRequest(new { message = "Bu email adresi zaten kullanılıyor" });

        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // 1. Workspace oluştur
            var createWorkspaceCommand = new CreateWorkspaceCommand
            {
                WorkspaceName = request.CompanyName ?? (request.FullName + "'s Company"),
                AdminUserEmail = request.Email,
                AdminUserFullName = request.FullName
            };

            var workspace = new Data.Workspace.Workspace(createWorkspaceCommand);
            
            // Firma email ve telefon bilgilerini ekle
            if (!string.IsNullOrEmpty(request.CompanyEmail))
            {
                workspace.Email = request.CompanyEmail;
            }
            if (!string.IsNullOrEmpty(request.CompanyPhone))
            {
                workspace.PhoneNumber = request.CompanyPhone;
            }
            
            _context.Workspaces.Add(workspace);
            await _context.SaveChangesAsync();

            // 2. User oluştur
            var user = new ApplicationUser(createWorkspaceCommand, workspace);
            user.UserName = request.Email;
            user.Email = request.Email;
            user.EmailConfirmed = true;
            
            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Admin");
                
                // DEFAULT TEMPLATE'LERİ EKLE
                var defaultTemplates = DefaultTemplates.GetDefaultTemplates(workspace.Id);
                _context.MessageTemplates.AddRange(defaultTemplates);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Added {defaultTemplates.Count} default templates for workspace {workspace.Id}");
                
                await transaction.CommitAsync();

                // 3. Welcome email gönder
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

                // TOKEN'I TokenService İLE OLUŞTUR - DÜZELTILDI
                var tokenModel = _tokenService.Create(user);
                
                return Ok(new MeTokenModel
                {
                    BearerToken = tokenModel.BearerToken, // DÜZELTILDI
                    Me = new MeUserModel
                    {
                        Id = user.Id.ToString(),
                        Email = user.Email,
                        FullName = user.FullName,
                        PhoneNumber = user.PhoneNumber,
                        WorkspaceId = user.WorkspaceId,
                        WorkspaceName = workspace.Name,
                        IsAdmin = true,
                        IsDispatcher = false,
                        IsDriver = false,
                        IsSuperAdmin = false,
                        IsOnboarded = false,
                        DepotId = null
                    }
                });
            }

            await transaction.RollbackAsync();
            
            var errorMessages = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(new { 
                message = "Kayıt işlemi başarısız", 
                errors = errorMessages 
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during registration");
            return BadRequest(new { message = "Bir hata oluştu. Lütfen tekrar deneyin." });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Geçersiz istek formatı" });

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Güvenlik için kullanıcı bulunamadı demiyoruz
            return Ok(new { message = "Eğer email adresiniz sistemde kayıtlıysa, şifre sıfırlama linki gönderildi." });
        }

        // Token oluştur
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
        
        // Email gönder
        try
        {
            await _emailService.SendPasswordResetEmail(user.Email, encodedToken);
            _logger.LogInformation($"Password reset email sent to {user.Email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send password reset email to {user.Email}");
        }
        
        return Ok(new { message = "Şifre sıfırlama linki email adresinize gönderildi." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Geçersiz istek formatı" });

        try
        {
            // Token'ı decode et
            var decodedToken = Encoding.UTF8.GetString(Convert.FromBase64String(request.Token));
            
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Geçersiz işlem." });
            }

            // Şifreyi sıfırla
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
            
            if (result.Succeeded)
            {
                _logger.LogInformation($"Password reset successful for {user.Email}");
                return Ok(new { message = "Şifreniz başarıyla değiştirildi. Yeni şifrenizle giriş yapabilirsiniz." });
            }
            
            var errors = result.Errors.Select(e => e.Description).ToList();
            _logger.LogWarning($"Password reset failed for {user.Email}: {string.Join(", ", errors)}");
            
            return BadRequest(new { 
                message = "Şifre değiştirilemedi.", 
                errors = errors 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return BadRequest(new { message = "Şifre sıfırlama işlemi başarısız. Link süresi dolmuş olabilir." });
        }
    }

    [HttpGet("")]
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

        var roles = await _userManager.GetRolesAsync(user);
        var isSuperAdmin = roles.Contains("SuperAdmin");

        return Ok(new MeUserModel
        {
            Id = user.Id.ToString(),
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            WorkspaceId = user.WorkspaceId,
            WorkspaceName = user.Workspace?.Name,
            IsAdmin = user.IsAdmin,
            IsDispatcher = user.IsDispatcher,
            IsDriver = user.IsDriver,
            IsSuperAdmin = isSuperAdmin,
            IsOnboarded = user.IsOnboarded,
            DepotId = user.DepotId
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Çıkış yapıldı" });
    }

    // GenerateJwtToken METODU KALDIRILDI - ARTIK TokenService KULLANIYORUZ
}

// Request/Response Models
public class MeLoginRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class MeRegisterRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FullName { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyEmail { get; set; }
    public string? CompanyPhone { get; set; }
}

public class ForgotPasswordRequest
{
    public required string Email { get; set; }
}

public class ResetPasswordRequest
{
    public required string Email { get; set; }
    public required string Token { get; set; }
    public required string NewPassword { get; set; }
}

public class MeTokenModel
{
    public required string BearerToken { get; set; }
    public required MeUserModel Me { get; set; }
}

public class MeUserModel
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public int WorkspaceId { get; set; }
    public string? WorkspaceName { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsDispatcher { get; set; }
    public bool IsDriver { get; set; }
    public bool IsSuperAdmin { get; set; }
    public bool IsOnboarded { get; set; }
    public int? DepotId { get; set; }
}