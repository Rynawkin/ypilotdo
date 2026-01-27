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
    public class CustomerContactController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CustomerContactController> _logger;

        public CustomerContactController(AppDbContext context, ILogger<CustomerContactController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/CustomerContact/customer/{customerId}
        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult<List<CustomerContact>>> GetContactsByCustomerId(int customerId)
        {
            try
            {
                var workspaceId = User.GetWorkspaceId();
                
                var contacts = await _context.CustomerContacts
                    .Where(c => c.CustomerId == customerId && c.WorkspaceId == workspaceId && c.IsActive)
                    .OrderBy(c => c.Role)
                    .ThenBy(c => c.FirstName)
                    .ToListAsync();

                return Ok(contacts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contacts for customer {CustomerId}", customerId);
                return StatusCode(500, "İletişim kişileri alınırken hata oluştu");
            }
        }

        // GET: api/CustomerContact/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerContact>> GetContact(int id)
        {
            try
            {
                var workspaceId = User.GetWorkspaceId();
                
                var contact = await _context.CustomerContacts
                    .FirstOrDefaultAsync(c => c.Id == id && c.WorkspaceId == workspaceId);

                if (contact == null)
                {
                    return NotFound("İletişim kişisi bulunamadı");
                }

                return Ok(contact);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contact {ContactId}", id);
                return StatusCode(500, "İletişim kişisi alınırken hata oluştu");
            }
        }

        // POST: api/CustomerContact
        [HttpPost]
        public async Task<ActionResult<CustomerContact>> CreateContact(CustomerContact contact)
        {
            try
            {
                var workspaceId = User.GetWorkspaceId();
                var userId = User.GetUserId();

                // Müşterinin aynı workspace'te olduğunu kontrol et
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == contact.CustomerId && c.WorkspaceId == workspaceId);
                
                if (customer == null)
                {
                    return BadRequest("Geçersiz müşteri");
                }

                // Aynı email adresi varsa kontrol et
                var existingContact = await _context.CustomerContacts
                    .FirstOrDefaultAsync(c => c.CustomerId == contact.CustomerId && 
                                           c.Email.ToLower() == contact.Email.ToLower() && 
                                           c.IsActive);
                
                if (existingContact != null)
                {
                    return BadRequest("Bu email adresi zaten kullanılıyor");
                }

                // Primary contact kontrolü
                if (contact.IsPrimary)
                {
                    // Diğer primary contact'ları kaldır
                    var otherPrimaryContacts = await _context.CustomerContacts
                        .Where(c => c.CustomerId == contact.CustomerId && c.IsPrimary)
                        .ToListAsync();
                    
                    foreach (var otherContact in otherPrimaryContacts)
                    {
                        otherContact.IsPrimary = false;
                    }
                }

                contact.WorkspaceId = workspaceId;
                // contact.CreatedBy = (int?)userId; // Guid to int conversion not possible, skip for now
                contact.CreatedAt = DateTime.UtcNow;

                // Rol bazlı varsayılan bildirim ayarları
                SetDefaultNotificationSettings(contact);

                _context.CustomerContacts.Add(contact);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Customer contact created: {ContactId} for customer {CustomerId}", 
                    contact.Id, contact.CustomerId);

                return CreatedAtAction(nameof(GetContact), new { id = contact.Id }, contact);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer contact");
                return StatusCode(500, "İletişim kişisi oluşturulurken hata oluştu");
            }
        }

        // PUT: api/CustomerContact/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContact(int id, CustomerContact contact)
        {
            try
            {
                var workspaceId = User.GetWorkspaceId();
                var userId = User.GetUserId();

                if (id != contact.Id)
                {
                    return BadRequest("ID eşleşmiyor");
                }

                var existingContact = await _context.CustomerContacts
                    .FirstOrDefaultAsync(c => c.Id == id && c.WorkspaceId == workspaceId);

                if (existingContact == null)
                {
                    return NotFound("İletişim kişisi bulunamadı");
                }

                // Email benzersizlik kontrolü (kendisi hariç)
                var duplicateEmail = await _context.CustomerContacts
                    .AnyAsync(c => c.CustomerId == contact.CustomerId && 
                              c.Email.ToLower() == contact.Email.ToLower() && 
                              c.Id != id && c.IsActive);
                
                if (duplicateEmail)
                {
                    return BadRequest("Bu email adresi zaten kullanılıyor");
                }

                // Primary contact kontrolü
                if (contact.IsPrimary && !existingContact.IsPrimary)
                {
                    // Diğer primary contact'ları kaldır
                    var otherPrimaryContacts = await _context.CustomerContacts
                        .Where(c => c.CustomerId == contact.CustomerId && c.IsPrimary && c.Id != id)
                        .ToListAsync();
                    
                    foreach (var otherContact in otherPrimaryContacts)
                    {
                        otherContact.IsPrimary = false;
                    }
                }

                // Güncelle
                existingContact.FirstName = contact.FirstName;
                existingContact.LastName = contact.LastName;
                existingContact.Email = contact.Email;
                existingContact.Phone = contact.Phone;
                existingContact.Role = contact.Role;
                existingContact.IsActive = contact.IsActive;
                existingContact.IsPrimary = contact.IsPrimary;
                existingContact.ReceiveJourneyStart = contact.ReceiveJourneyStart;
                existingContact.ReceiveJourneyCheckIn = contact.ReceiveJourneyCheckIn;
                existingContact.ReceiveDeliveryCompleted = contact.ReceiveDeliveryCompleted;
                existingContact.ReceiveDeliveryFailed = contact.ReceiveDeliveryFailed;
                existingContact.ReceiveJourneyAssigned = contact.ReceiveJourneyAssigned;
                existingContact.ReceiveJourneyCancelled = contact.ReceiveJourneyCancelled;
                // existingContact.UpdatedBy = (int?)userId; // Guid to int conversion not possible, skip for now
                existingContact.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Customer contact updated: {ContactId}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer contact {ContactId}", id);
                return StatusCode(500, "İletişim kişisi güncellenirken hata oluştu");
            }
        }

        // DELETE: api/CustomerContact/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContact(int id)
        {
            try
            {
                var workspaceId = User.GetWorkspaceId();

                var contact = await _context.CustomerContacts
                    .FirstOrDefaultAsync(c => c.Id == id && c.WorkspaceId == workspaceId);

                if (contact == null)
                {
                    return NotFound("İletişim kişisi bulunamadı");
                }

                // Soft delete
                contact.IsActive = false;
                // contact.UpdatedBy = (int?)User.GetUserId(); // Guid to int conversion not possible, skip for now
                contact.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Customer contact deleted: {ContactId}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer contact {ContactId}", id);
                return StatusCode(500, "İletişim kişisi silinirken hata oluştu");
            }
        }

        // GET: api/CustomerContact/roles
        [HttpGet("roles")]
        public ActionResult<List<KeyValuePair<string, string>>> GetRoles()
        {
            return Ok(CustomerContact.Roles.GetRoleOptions());
        }

        // POST: api/CustomerContact/bulk-create/{customerId}
        [HttpPost("bulk-create/{customerId}")]
        public async Task<ActionResult> BulkCreateDefaultContacts(int customerId, [FromBody] List<CustomerContact> contacts)
        {
            try
            {
                var workspaceId = User.GetWorkspaceId();
                var userId = User.GetUserId();

                // Müşterinin varlığını kontrol et
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == customerId && c.WorkspaceId == workspaceId);
                
                if (customer == null)
                {
                    return BadRequest("Geçersiz müşteri");
                }

                foreach (var contact in contacts)
                {
                    contact.CustomerId = customerId;
                    contact.WorkspaceId = workspaceId;
                    // contact.CreatedBy = (int?)userId; // Guid to int conversion not possible, skip for now
                    contact.CreatedAt = DateTime.UtcNow;
                    SetDefaultNotificationSettings(contact);
                }

                _context.CustomerContacts.AddRange(contacts);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Bulk created {Count} contacts for customer {CustomerId}", 
                    contacts.Count, customerId);

                return Ok(new { message = $"{contacts.Count} kişi başarıyla eklendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk creating contacts for customer {CustomerId}", customerId);
                return StatusCode(500, "Kişiler oluşturulurken hata oluştu");
            }
        }

        private void SetDefaultNotificationSettings(CustomerContact contact)
        {
            // Depo Sorumlusu ve Satınalma Sorumlusu için varsayılan olarak tüm bildirimler açık
            if (contact.Role == CustomerContact.Roles.DepoSorumlusu || 
                contact.Role == CustomerContact.Roles.SatinalmasorumluSu)
            {
                contact.ReceiveJourneyStart = true;
                contact.ReceiveJourneyCheckIn = true;
                contact.ReceiveDeliveryCompleted = true;
                contact.ReceiveDeliveryFailed = true;
                contact.ReceiveJourneyAssigned = true;
                contact.ReceiveJourneyCancelled = true;
            }
            // Muhasebe Sorumlusu için sadece tamamlama bildirimleri
            else if (contact.Role == CustomerContact.Roles.MuhasebeSorumlusu)
            {
                contact.ReceiveJourneyStart = false;
                contact.ReceiveJourneyCheckIn = false;
                contact.ReceiveDeliveryCompleted = true;
                contact.ReceiveDeliveryFailed = true;
                contact.ReceiveJourneyAssigned = false;
                contact.ReceiveJourneyCancelled = true;
            }
            // Diğer roller için varsayılan ayarlar
            else
            {
                contact.ReceiveJourneyStart = true;
                contact.ReceiveJourneyCheckIn = false;
                contact.ReceiveDeliveryCompleted = true;
                contact.ReceiveDeliveryFailed = true;
                contact.ReceiveJourneyAssigned = false;
                contact.ReceiveJourneyCancelled = false;
            }
        }
    }
}