using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.LocationUpdateRequests
{
    public class ApproveLocationUpdateRequestCommand : BaseAuthenticatedCommand<bool>
    {
        [JsonIgnore] public override bool RequiresDispatcher => true; // Sadece Dispatcher ve üzeri
        public int RequestId { get; set; }
        public bool UpdateFutureStops { get; set; } = true;
    }

    public class ApproveLocationUpdateRequestCommandHandler
        : BaseAuthenticatedCommandHandler<ApproveLocationUpdateRequestCommand, bool>
    {
        private readonly AppDbContext _context;

        public ApproveLocationUpdateRequestCommandHandler(AppDbContext context, IUserService userService)
            : base(userService)
        {
            _context = context;
        }

        protected override async Task<bool> HandleCommand(ApproveLocationUpdateRequestCommand request, CancellationToken cancellationToken)
        {
            var locationRequest = await _context.LocationUpdateRequests
                .Include(lr => lr.Customer)
                .FirstOrDefaultAsync(lr =>
                    lr.Id == request.RequestId &&
                    lr.WorkspaceId == User.WorkspaceId &&
                    lr.Status == "Pending",
                    cancellationToken);

            if (locationRequest == null)
                throw new InvalidOperationException("Konum güncelleme talebi bulunamadı veya zaten işlenmiş.");

            // Müşteri konumunu güncelle
            if (locationRequest.Customer != null)
            {
                // Customer.Latitude/Longitude çoğu modelde double oluyor; uyum için explicit cast kullandık
                locationRequest.Customer.Latitude = (double)locationRequest.RequestedLatitude;
                locationRequest.Customer.Longitude = (double)locationRequest.RequestedLongitude;
                locationRequest.Customer.Address = locationRequest.RequestedAddress;
                locationRequest.Customer.UpdatedAt = DateTime.UtcNow;
            }

            // Talebi onayla
            locationRequest.Status = "Approved";
            locationRequest.ApprovedById = User.Id.ToString();
            locationRequest.ApprovedByName = User.FullName ?? User.Email;
            locationRequest.ProcessedAt = DateTime.UtcNow;
            locationRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
