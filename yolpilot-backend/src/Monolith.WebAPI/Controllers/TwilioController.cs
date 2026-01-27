using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;

namespace Monolith.WebAPI.Controllers
{
    [ApiController]
    [Route("api/twilio")]
    [Authorize]
    public class TwilioController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TwilioController> _logger;
        private readonly IConfiguration _configuration;

        public TwilioController(
            AppDbContext context,
            ILogger<TwilioController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        // Twilio Embedded Signup başlat
        [HttpGet("embedded-signup")]
        public async Task<IActionResult> GetEmbeddedSignupUrl()
        {
            // HttpContext'ten workspace ID'yi al
            var workspaceIdClaim = HttpContext.User.FindFirst("WorkspaceId")?.Value;
            if (string.IsNullOrEmpty(workspaceIdClaim) || !int.TryParse(workspaceIdClaim, out var workspaceId))
            {
                return Unauthorized("Workspace not found in token");
            }

            // Workspace'i kontrol et
            var workspace = await _context.Workspaces
                .FirstOrDefaultAsync(w => w.Id == workspaceId);
                
            if (workspace == null) return NotFound("Workspace not found");

            // Twilio Embedded Signup URL'i oluştur
            var callbackUrl = $"{_configuration["AppUrl"]}/api/twilio/callback";
            var signupUrl = $"https://www.twilio.com/console/whatsapp/embedded-signup" +
                           $"?callback_url={Uri.EscapeDataString(callbackUrl)}" +
                           $"&workspace_id={workspace.Id}";

            return Ok(new { signupUrl });
        }

        // Twilio callback - workspace'e credentials kaydet
        [HttpPost("callback")]
        [AllowAnonymous] // Twilio'dan gelecek
        public async Task<IActionResult> TwilioCallback([FromBody] TwilioCallbackDto dto)
        {
            try
            {
                var workspace = await _context.Workspaces
                    .FirstOrDefaultAsync(w => w.Id == dto.WorkspaceId);
                    
                if (workspace == null) return NotFound();

                // Workspace metodlarını kullan
                workspace.ConnectTwilio(
                    dto.AccountSid,
                    EncryptString(dto.AuthToken),
                    dto.PhoneNumber,
                    dto.UseSandbox ?? false
                );

                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"WhatsApp connected for workspace {workspace.Id}");
                
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Twilio callback");
                return StatusCode(500);
            }
        }

        // WhatsApp bağlantısını kaldır
        [HttpDelete("disconnect")]
        public async Task<IActionResult> DisconnectWhatsApp()
        {
            var workspaceIdClaim = HttpContext.User.FindFirst("WorkspaceId")?.Value;
            if (string.IsNullOrEmpty(workspaceIdClaim) || !int.TryParse(workspaceIdClaim, out var workspaceId))
            {
                return Unauthorized("Workspace not found in token");
            }

            var workspace = await _context.Workspaces
                .FirstOrDefaultAsync(w => w.Id == workspaceId);
                
            if (workspace == null) return NotFound();

            workspace.DisconnectTwilio();

            await _context.SaveChangesAsync();
            
            return Ok(new { success = true });
        }

        // Bağlantı durumunu kontrol et
        [HttpGet("status")]
        public async Task<IActionResult> GetConnectionStatus()
        {
            var workspaceIdClaim = HttpContext.User.FindFirst("WorkspaceId")?.Value;
            if (string.IsNullOrEmpty(workspaceIdClaim) || !int.TryParse(workspaceIdClaim, out var workspaceId))
            {
                return Unauthorized("Workspace not found in token");
            }

            var workspace = await _context.Workspaces
                .FirstOrDefaultAsync(w => w.Id == workspaceId);
                
            if (workspace == null) return NotFound();

            return Ok(new
            {
                connected = workspace.TwilioVerified,
                phoneNumber = workspace.TwilioWhatsAppNumber,
                connectedAt = workspace.TwilioConnectedAt
            });
        }

        // Basit şifreleme (Production'da daha güçlü bir yöntem kullanın)
        private string EncryptString(string plainText)
        {
            var key = _configuration["Encryption:Key"] ?? "DefaultKey123456"; // 16 karakter
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
            aes.IV = new byte[16];
            
            var encryptor = aes.CreateEncryptor();
            var encrypted = encryptor.TransformFinalBlock(
                Encoding.UTF8.GetBytes(plainText), 0, plainText.Length);
            
            return Convert.ToBase64String(encrypted);
        }
    }

    // DTO for Twilio callback
    public class TwilioCallbackDto
    {
        public int WorkspaceId { get; set; }
        public string AccountSid { get; set; } = string.Empty;
        public string AuthToken { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool? UseSandbox { get; set; }
    }
}