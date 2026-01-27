using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Marketing;

namespace Monolith.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("MarketingCors")]
    public class MarketingLeadController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MarketingLeadController> _logger;

        public MarketingLeadController(AppDbContext context, ILogger<MarketingLeadController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // POST: api/MarketingLead - Public endpoint for marketing forms
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<MarketingLead>> CreateLead(CreateMarketingLeadRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Name) ||
                    string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Company))
                {
                    return BadRequest("Ad, email ve şirket adı zorunludur.");
                }

                // Check for duplicate email in last 24 hours to prevent spam
                var recentLead = await _context.MarketingLeads
                    .Where(l => l.Email.ToLower() == request.Email.ToLower() &&
                               l.CreatedAt > DateTime.UtcNow.AddDays(-1))
                    .FirstOrDefaultAsync();

                if (recentLead != null)
                {
                    return BadRequest("Bu email adresi son 24 saat içinde kullanılmış. Lütfen daha sonra tekrar deneyin.");
                }

                var lead = new MarketingLead
                {
                    Name = request.Name.Trim(),
                    Email = request.Email.Trim().ToLower(),
                    Company = request.Company.Trim(),
                    Phone = request.Phone?.Trim(),
                    VehicleCount = request.VehicleCount?.Trim(),
                    Message = request.Message?.Trim(),
                    Source = request.Source ?? "website",
                    SelectedPlan = request.SelectedPlan?.Trim(),
                    Status = LeadStatus.New
                };

                _context.MarketingLeads.Add(lead);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New marketing lead created: {Email} from {Company}",
                    lead.Email, lead.Company);

                return Ok(new {
                    message = "Talebiniz başarıyla gönderildi. En kısa sürede size dönüş yapacağız.",
                    leadId = lead.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating marketing lead");
                return StatusCode(500, "İsteğiniz işlenirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
            }
        }

        // GET: api/MarketingLead - SuperAdmin only
        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<List<MarketingLead>>> GetLeads(
            [FromQuery] LeadStatus? status = null,
            [FromQuery] string? source = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.MarketingLeads.AsQueryable();

                if (status.HasValue)
                {
                    query = query.Where(l => l.Status == status.Value);
                }

                if (!string.IsNullOrEmpty(source))
                {
                    query = query.Where(l => l.Source == source);
                }

                var leads = await query
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(leads);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching marketing leads");
                return StatusCode(500, "Lead'ler alınırken hata oluştu");
            }
        }

        // GET: api/MarketingLead/{id} - SuperAdmin only
        [HttpGet("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<MarketingLead>> GetLead(int id)
        {
            try
            {
                var lead = await _context.MarketingLeads
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (lead == null)
                {
                    return NotFound("Lead bulunamadı");
                }

                return Ok(lead);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching marketing lead {LeadId}", id);
                return StatusCode(500, "Lead alınırken hata oluştu");
            }
        }

        // PUT: api/MarketingLead/{id} - SuperAdmin only
        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateLead(int id, UpdateMarketingLeadRequest request)
        {
            try
            {
                var lead = await _context.MarketingLeads
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (lead == null)
                {
                    return NotFound("Lead bulunamadı");
                }

                lead.Status = request.Status;
                lead.AdminNotes = request.AdminNotes;
                lead.AssignedTo = request.AssignedTo;
                lead.UpdatedAt = DateTime.UtcNow;

                if (request.Status == LeadStatus.Contacted && !lead.ContactedAt.HasValue)
                {
                    lead.ContactedAt = DateTime.UtcNow;
                }

                if ((request.Status == LeadStatus.Won || request.Status == LeadStatus.Lost ||
                     request.Status == LeadStatus.Archived) && !lead.ClosedAt.HasValue)
                {
                    lead.ClosedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Marketing lead {LeadId} updated by admin", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating marketing lead {LeadId}", id);
                return StatusCode(500, "Lead güncellenirken hata oluştu");
            }
        }

        // GET: api/MarketingLead/stats - SuperAdmin only
        [HttpGet("stats")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> GetLeadStats()
        {
            try
            {
                var totalLeads = await _context.MarketingLeads.CountAsync();
                var newLeads = await _context.MarketingLeads.CountAsync(l => l.Status == LeadStatus.New);
                var qualifiedLeads = await _context.MarketingLeads.CountAsync(l => l.Status == LeadStatus.Qualified);
                var wonLeads = await _context.MarketingLeads.CountAsync(l => l.Status == LeadStatus.Won);
                var thisMonthLeads = await _context.MarketingLeads
                    .CountAsync(l => l.CreatedAt >= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1));

                var conversionRate = totalLeads > 0 ? (double)wonLeads / totalLeads * 100 : 0;

                return Ok(new
                {
                    totalLeads,
                    newLeads,
                    qualifiedLeads,
                    wonLeads,
                    thisMonthLeads,
                    conversionRate = Math.Round(conversionRate, 2)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching lead stats");
                return StatusCode(500, "İstatistikler alınırken hata oluştu");
            }
        }
    }

    public class CreateMarketingLeadRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? VehicleCount { get; set; }
        public string? Message { get; set; }
        public string? Source { get; set; }
        public string? SelectedPlan { get; set; }
    }

    public class UpdateMarketingLeadRequest
    {
        public LeadStatus Status { get; set; }
        public string? AdminNotes { get; set; }
        public string? AssignedTo { get; set; }
    }
}