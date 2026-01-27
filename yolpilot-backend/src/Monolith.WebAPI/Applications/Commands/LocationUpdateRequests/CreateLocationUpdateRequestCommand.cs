using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.LocationUpdateRequests
{
    public class CreateLocationUpdateRequestCommand : BaseAuthenticatedCommand<int>
    {
        [JsonIgnore] public override bool RequiresDispatcher => false; // Driver da oluşturabilir

        public int JourneyId { get; set; }
        public int StopId { get; set; }
        public int CustomerId { get; set; }

        public decimal CurrentLatitude { get; set; }
        public decimal CurrentLongitude { get; set; }
        public string CurrentAddress { get; set; } = string.Empty;

        public decimal RequestedLatitude { get; set; }
        public decimal RequestedLongitude { get; set; }

        public string Reason { get; set; } = string.Empty;
    }

    public class CreateLocationUpdateRequestCommandHandler : BaseAuthenticatedCommandHandler<CreateLocationUpdateRequestCommand, int>
    {
        private readonly AppDbContext _context;

        public CreateLocationUpdateRequestCommandHandler(AppDbContext context, IUserService userService)
            : base(userService)
        {
            _context = context;
        }

        protected override async Task<int> HandleCommand(CreateLocationUpdateRequestCommand request, CancellationToken cancellationToken)
        {
            // Journey'nin workspace'e ait olduğunu kontrol et
            var journey = await _context.Journeys
                .FirstOrDefaultAsync(j => j.Id == request.JourneyId && j.WorkspaceId == User.WorkspaceId, cancellationToken);

            if (journey == null)
            {
                throw new InvalidOperationException("Journey bulunamadı veya yetkisiz erişim.");
            }

            // İstekte adres boş kalabilir; biz koordinatları yazmayalım (UI zaten gösterebilir/gizleyebilir)
            var requestedAddress = ""; // İstersek gerçek reverse-geocoding ile doldururuz

            var locationRequest = new LocationUpdateRequest
            {
                WorkspaceId = User.WorkspaceId,
                JourneyId = request.JourneyId,
                JourneyStopId = request.StopId,
                CustomerId = request.CustomerId,

                CurrentLatitude = request.CurrentLatitude,
                CurrentLongitude = request.CurrentLongitude,
                CurrentAddress = request.CurrentAddress,

                RequestedLatitude = request.RequestedLatitude,
                RequestedLongitude = request.RequestedLongitude,
                RequestedAddress = requestedAddress,

                Reason = request.Reason,
                Status = "Pending",
                RequestedById = User.Id.ToString(), // Guid to string
                RequestedByName = User.FullName ?? User.Email,
                CreatedAt = DateTime.UtcNow
            };

            _context.LocationUpdateRequests.Add(locationRequest);
            await _context.SaveChangesAsync(cancellationToken);

            return locationRequest.Id;
        }
    }
}
