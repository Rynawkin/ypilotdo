using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Customers
{
    public class CreateCustomerCommand : BaseAuthenticatedCommand<CustomerResponse>
    {
        [JsonIgnore] public override bool RequiresDispatcher => true;
        
        public string Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; }
        
        // WhatsApp Alanları - YENİ EKLENEN
        public string WhatsApp { get; set; }
        public bool WhatsAppOptIn { get; set; } = false;
        public bool WhatsAppVerified { get; set; } = false;
        public DateTime? WhatsAppOptInDate { get; set; }
        
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Priority { get; set; } = "normal"; // high, normal, low
        public int EstimatedServiceTime { get; set; } = 10; // dakika
        public string Notes { get; set; }
        public List<string> Tags { get; set; }
        public TimeWindowDto TimeWindow { get; set; }
    }

    public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
    {
        public CreateCustomerCommandValidator()
        {
            RuleFor(x => x.Latitude)
                .NotEqual(0).WithMessage("Latitude is required")
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90");

            RuleFor(x => x.Longitude)
                .NotEqual(0).WithMessage("Longitude is required")
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180");
        }
    }

    public class CreateCustomerCommandHandler : BaseAuthenticatedCommandHandler<CreateCustomerCommand, CustomerResponse>
    {
        private readonly AppDbContext _context;

        public CreateCustomerCommandHandler(AppDbContext context, IUserService userService) 
            : base(userService)
        {
            _context = context;
        }

        protected override async Task<CustomerResponse> HandleCommand(CreateCustomerCommand request, CancellationToken cancellationToken)
        {
            // Müşteri kodu otomatik oluştur (eğer belirtilmemişse)
            if (string.IsNullOrEmpty(request.Code))
            {
                // Sadece silinmemiş müşterileri say
                var activeCustomerCount = await _context.Customers
                    .Where(c => c.WorkspaceId == User.WorkspaceId && !c.IsDeleted)
                    .CountAsync(cancellationToken);
                
                // Benzersiz bir kod bulana kadar döngü
                var codeGenerated = false;
                var attemptCount = 0;
                var maxAttempts = 100; // Sonsuz döngüyü önlemek için
                
                while (!codeGenerated && attemptCount < maxAttempts)
                {
                    attemptCount++;
                    var candidateCode = $"MUS{(activeCustomerCount + attemptCount).ToString("D3")}";
                    
                    // Bu kodun kullanılıp kullanılmadığını kontrol et (silinmiş kayıtlar dahil)
                    var codeExists = await _context.Customers
                        .Where(c => c.WorkspaceId == User.WorkspaceId && c.Code == candidateCode)
                        .AnyAsync(cancellationToken);
                    
                    if (!codeExists)
                    {
                        request.Code = candidateCode;
                        codeGenerated = true;
                    }
                }
                
                if (!codeGenerated)
                {
                    throw new InvalidOperationException("Müşteri kodu oluşturulamadı. Lütfen manuel olarak bir kod girin.");
                }
            }
            else
            {
                // Manuel girilen kodun benzersizliğini kontrol et
                // Workspace içinde kod benzersiz olmalı (silinmiş kayıtlar dahil kontrol edilmeli)
                var existingCustomer = await _context.Customers
                    .Where(c => c.WorkspaceId == User.WorkspaceId && 
                               c.Code == request.Code && 
                               !c.IsDeleted) // Sadece aktif kayıtlarda kontrol et
                    .FirstOrDefaultAsync(cancellationToken);
                
                if (existingCustomer != null)
                {
                    throw new InvalidOperationException($"Bu müşteri kodu zaten kullanılıyor: {request.Code}");
                }
                
                // Silinmiş kayıtlarda da kontrol et ve uyarı ver
                var deletedCustomer = await _context.Customers
                    .Where(c => c.WorkspaceId == User.WorkspaceId && 
                               c.Code == request.Code && 
                               c.IsDeleted)
                    .FirstOrDefaultAsync(cancellationToken);
                
                if (deletedCustomer != null)
                {
                    // Silinmiş kayıt varsa, onu tekrar aktif hale getirmeyi öner
                    throw new InvalidOperationException($"Bu müşteri kodu ({request.Code}) daha önce kullanılmış ve silinmiş. " +
                        $"Farklı bir kod kullanın veya eski müşteriyi geri yükleyin.");
                }
            }

            var customer = new Customer
            {
                WorkspaceId = User.WorkspaceId,
                Code = request.Code,
                Name = request.Name,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email,
                
                // WhatsApp Alanları - YENİ EKLENEN
                WhatsApp = request.WhatsApp ?? request.Phone, // WhatsApp yoksa Phone'u kullan
                WhatsAppOptIn = request.WhatsAppOptIn,
                WhatsAppVerified = request.WhatsAppVerified,
                WhatsAppOptInDate = request.WhatsAppOptIn ? (request.WhatsAppOptInDate ?? DateTime.UtcNow) : null,
                
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Priority = request.Priority,
                EstimatedServiceTime = request.EstimatedServiceTime,
                Notes = request.Notes,
                Tags = request.Tags != null ? string.Join(",", request.Tags) : null,
                TimeWindowStart = ParseTimeWindow(request.TimeWindow?.Start),
                TimeWindowEnd = ParseTimeWindow(request.TimeWindow?.End),
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
            
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                // Database seviyesinde unique constraint ihlali durumunda
                if (ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true ||
                    ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true)
                {
                    throw new InvalidOperationException($"Bu müşteri kodu zaten kullanılıyor: {request.Code}");
                }
                throw;
            }

            return CustomerMapper.ToResponse(customer);
        }

        private static TimeSpan? ParseTimeWindow(string timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString))
                return null;

            // "HH:mm" formatında parse et
            if (TimeSpan.TryParseExact(timeString.Trim(), @"hh\:mm", null, out var timeSpan))
                return timeSpan;

            // Alternatif formatlar da dene
            if (TimeSpan.TryParse(timeString.Trim(), out timeSpan))
                return timeSpan;

            // Parse edilemezse null dön
            return null;
        }
    }
}
