using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Customers
{
    public class UpdateCustomerCommand : BaseAuthenticatedCommand<CustomerResponse>
    {
        [JsonIgnore] public override bool RequiresDispatcher => true;
        
        public int Id { get; set; }
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
        public string Priority { get; set; } = "normal";
        public int EstimatedServiceTime { get; set; } = 10;
        public string Notes { get; set; }
        public List<string> Tags { get; set; }
        public TimeWindowDto TimeWindow { get; set; }
    }

    public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
    {
        public UpdateCustomerCommandValidator()
        {
            RuleFor(x => x.Latitude)
                .NotEqual(0).WithMessage("Latitude is required")
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90");

            RuleFor(x => x.Longitude)
                .NotEqual(0).WithMessage("Longitude is required")
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180");
        }
    }

    public class UpdateCustomerCommandHandler : BaseAuthenticatedCommandHandler<UpdateCustomerCommand, CustomerResponse>
    {
        private readonly AppDbContext _context;

        public UpdateCustomerCommandHandler(AppDbContext context, IUserService userService)
            : base(userService)
        {
            _context = context;
        }

        protected override async Task<CustomerResponse> HandleCommand(UpdateCustomerCommand request, CancellationToken cancellationToken)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == request.Id && c.WorkspaceId == User.WorkspaceId, cancellationToken);

            if (customer == null)
            {
                return null;
            }

            // Kod değiştirildiyse benzersizliği kontrol et
            if (!string.IsNullOrEmpty(request.Code) && request.Code != customer.Code)
            {
                var existingCustomer = await _context.Customers
                    .Where(c => c.WorkspaceId == User.WorkspaceId 
                        && c.Code == request.Code 
                        && c.Id != request.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingCustomer != null)
                {
                    throw new InvalidOperationException($"Bu müşteri kodu zaten kullanılıyor: {request.Code}");
                }
                customer.Code = request.Code;
            }

            // Güncelleme işlemleri
            customer.Name = request.Name;
            customer.Address = request.Address;
            customer.Phone = request.Phone;
            customer.Email = request.Email;
            
            // WhatsApp Alanları - YENİ EKLENEN
            customer.WhatsApp = request.WhatsApp ?? request.Phone;
            
            // Opt-in durumu değişti mi kontrol et
            var previousOptIn = customer.WhatsAppOptIn;
            customer.WhatsAppOptIn = request.WhatsAppOptIn;
            customer.WhatsAppVerified = request.WhatsAppVerified;
            
            // Opt-in yeni açıldıysa tarihi kaydet
            if (request.WhatsAppOptIn && !previousOptIn)
            {
                customer.WhatsAppOptInDate = DateTime.UtcNow;
            }
            // Opt-in kapatıldıysa tarihi temizle
            else if (!request.WhatsAppOptIn)
            {
                customer.WhatsAppOptInDate = null;
                customer.WhatsAppVerified = false; // Opt-in kapatıldığında doğrulamayı da sıfırla
            }
            // Opt-in açık ve tarih verilmişse güncelle
            else if (request.WhatsAppOptIn && request.WhatsAppOptInDate.HasValue)
            {
                customer.WhatsAppOptInDate = request.WhatsAppOptInDate;
            }
            
            customer.Latitude = request.Latitude;
            customer.Longitude = request.Longitude;
            customer.Priority = request.Priority;
            customer.EstimatedServiceTime = request.EstimatedServiceTime;
            customer.Notes = request.Notes;
            customer.Tags = request.Tags != null ? string.Join(",", request.Tags) : null;
            customer.TimeWindowStart = ParseTimeWindow(request.TimeWindow?.Start);
            customer.TimeWindowEnd = ParseTimeWindow(request.TimeWindow?.End);
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

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
