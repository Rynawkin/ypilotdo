using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Extensions;

namespace Monolith.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationRoleMappingController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<NotificationRoleMappingController> _logger;

        public NotificationRoleMappingController(AppDbContext context, ILogger<NotificationRoleMappingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/NotificationRoleMapping
        [HttpGet]
        public async Task<ActionResult<List<NotificationRoleMapping>>> GetAllMappings()
        {
            try
            {
                var workspaceId = User.GetWorkspaceId();
                
                var mappings = await _context.NotificationRoleMappings
                    .Where(m => m.WorkspaceId == workspaceId)
                    .OrderBy(m => m.ContactRole)
                    .ThenBy(m => m.NotificationType)
                    .ToListAsync();

                return Ok(mappings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification role mappings");
                return StatusCode(500, "Bildirim rol eşleştirmeleri alınırken hata oluştu");
            }
        }

        // GET: api/NotificationRoleMapping/roles
        [HttpGet("roles")]
        public ActionResult<List<KeyValuePair<string, string>>> GetRoles()
        {
            return Ok(CustomerContact.Roles.GetRoleOptions());
        }

        // GET: api/NotificationRoleMapping/notification-types
        [HttpGet("notification-types")]
        public ActionResult<List<KeyValuePair<string, string>>> GetNotificationTypes()
        {
            return Ok(NotificationRoleMapping.NotificationTypes.GetTypeOptions());
        }

        // POST: api/NotificationRoleMapping/bulk-update
        [HttpPost("bulk-update")]
        public async Task<IActionResult> BulkUpdateMappings([FromBody] List<NotificationRoleMapping> mappings)
        {
            try
            {
                var workspaceId = User.GetWorkspaceId();
                
                // Mevcut tüm mappings'leri sil
                var existingMappings = await _context.NotificationRoleMappings
                    .Where(m => m.WorkspaceId == workspaceId)
                    .ToListAsync();
                
                _context.NotificationRoleMappings.RemoveRange(existingMappings);

                // Yeni mappings'leri ekle
                foreach (var mapping in mappings.Where(m => m.IsEnabled))
                {
                    mapping.WorkspaceId = workspaceId;
                    mapping.CreatedAt = DateTime.UtcNow;
                    mapping.UpdatedAt = DateTime.UtcNow;
                }

                _context.NotificationRoleMappings.AddRange(mappings.Where(m => m.IsEnabled));
                await _context.SaveChangesAsync();

                _logger.LogInformation("Bulk updated {Count} notification role mappings for workspace {WorkspaceId}", 
                    mappings.Count, workspaceId);

                return Ok(new { message = "Bildirim ayarları başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk updating notification role mappings");
                return StatusCode(500, "Bildirim ayarları güncellenirken hata oluştu");
            }
        }

        // POST: api/NotificationRoleMapping/reset-defaults
        [HttpPost("reset-defaults")]
        public async Task<IActionResult> ResetToDefaults()
        {
            try
            {
                var workspaceId = User.GetWorkspaceId();
                
                // Mevcut tüm mappings'leri sil
                var existingMappings = await _context.NotificationRoleMappings
                    .Where(m => m.WorkspaceId == workspaceId)
                    .ToListAsync();
                
                _context.NotificationRoleMappings.RemoveRange(existingMappings);

                // Varsayılan mappings'leri oluştur
                var defaultMappings = CreateDefaultMappings(workspaceId);
                _context.NotificationRoleMappings.AddRange(defaultMappings);
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("Reset notification role mappings to defaults for workspace {WorkspaceId}", workspaceId);

                return Ok(new { message = "Bildirim ayarları varsayılana sıfırlandı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting notification role mappings to defaults");
                return StatusCode(500, "Bildirim ayarları sıfırlanırken hata oluştu");
            }
        }

        private List<NotificationRoleMapping> CreateDefaultMappings(int workspaceId)
        {
            var mappings = new List<NotificationRoleMapping>();
            var now = DateTime.UtcNow;

            // Depo Sorumlusu - Tüm bildirimler
            foreach (var notificationType in NotificationRoleMapping.NotificationTypes.GetAllTypes())
            {
                mappings.Add(new NotificationRoleMapping
                {
                    WorkspaceId = workspaceId,
                    ContactRole = CustomerContact.Roles.DepoSorumlusu,
                    NotificationType = notificationType,
                    IsEnabled = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            // Satınalma Sorumlusu - Tüm bildirimler
            foreach (var notificationType in NotificationRoleMapping.NotificationTypes.GetAllTypes())
            {
                mappings.Add(new NotificationRoleMapping
                {
                    WorkspaceId = workspaceId,
                    ContactRole = CustomerContact.Roles.SatinalmasorumluSu,
                    NotificationType = notificationType,
                    IsEnabled = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            // Muhasebe Sorumlusu - Sadece tamamlama bildirimleri
            var accountingNotifications = new[]
            {
                NotificationRoleMapping.NotificationTypes.DeliveryCompleted,
                NotificationRoleMapping.NotificationTypes.DeliveryFailed
            };

            foreach (var notificationType in accountingNotifications)
            {
                mappings.Add(new NotificationRoleMapping
                {
                    WorkspaceId = workspaceId,
                    ContactRole = CustomerContact.Roles.MuhasebeSorumlusu,
                    NotificationType = notificationType,
                    IsEnabled = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            // Diğer - Temel bildirimler
            var basicNotifications = new[]
            {
                NotificationRoleMapping.NotificationTypes.JourneyStart,
                NotificationRoleMapping.NotificationTypes.DeliveryCompleted,
                NotificationRoleMapping.NotificationTypes.DeliveryFailed
            };

            foreach (var notificationType in basicNotifications)
            {
                mappings.Add(new NotificationRoleMapping
                {
                    WorkspaceId = workspaceId,
                    ContactRole = CustomerContact.Roles.Diger,
                    NotificationType = notificationType,
                    IsEnabled = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            return mappings;
        }
    }
}