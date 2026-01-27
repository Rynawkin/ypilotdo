using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.LocationUpdateRequests
{
    public class RejectLocationUpdateRequestCommand : BaseAuthenticatedCommand<bool>
    {
        [JsonIgnore] public override bool RequiresDispatcher => true;
        
        public int RequestId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class RejectLocationUpdateRequestCommandHandler : BaseAuthenticatedCommandHandler<RejectLocationUpdateRequestCommand, bool>
    {
        private readonly AppDbContext _context;

        public RejectLocationUpdateRequestCommandHandler(AppDbContext context, IUserService userService)
            : base(userService)
        {
            _context = context;
        }

        protected override async Task<bool> HandleCommand(RejectLocationUpdateRequestCommand request, CancellationToken cancellationToken)
        {
            var locationRequest = await _context.LocationUpdateRequests
                .FirstOrDefaultAsync(lr => 
                    lr.Id == request.RequestId && 
                    lr.WorkspaceId == User.WorkspaceId && 
                    lr.Status == "Pending", 
                    cancellationToken);

            if (locationRequest == null)
            {
                throw new InvalidOperationException("Konum güncelleme talebi bulunamadı veya zaten işlenmiş.");
            }

            locationRequest.Status = "Rejected";
            locationRequest.RejectionReason = request.Reason;
            locationRequest.ApprovedById = User.Id.ToString(); // Guid to string
            locationRequest.ApprovedByName = User.FullName ?? User.Email;
            locationRequest.ProcessedAt = DateTime.UtcNow;
            locationRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}