using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Customers
{
    public class DeleteCustomerCommand : BaseAuthenticatedCommand<bool>
    {
        [JsonIgnore] public override bool RequiresAdmin => true;  // SECURITY: Only admins can delete customers

        public int Id { get; set; }
    }

    public class DeleteCustomerCommandHandler : BaseAuthenticatedCommandHandler<DeleteCustomerCommand, bool>
    {
        private readonly AppDbContext _context;

        public DeleteCustomerCommandHandler(AppDbContext context, IUserService userService)
            : base(userService)
        {
            _context = context;
        }

        protected override async Task<bool> HandleCommand(DeleteCustomerCommand request, CancellationToken cancellationToken)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == request.Id && c.WorkspaceId == User.WorkspaceId, cancellationToken);

            if (customer == null || customer.IsDeleted)
            {
                return false;
            }

            // NOT: RouteStop ile ilişki kontrolü şimdilik kaldırıldı
            // RouteStop entity'sinde CustomerId field'ı yoksa bu kontrol yapılamaz
            // İleride RouteStop entity'si düzenlendiğinde bu kontrol eklenebilir
            
            /*
            var hasRouteStops = await _context.RouteStops
                .AnyAsync(rs => rs.CustomerId == request.Id, cancellationToken);

            if (hasRouteStops)
            {
                throw new InvalidOperationException("Bu müşteri aktif rotalarda kullanılıyor ve silinemez.");
            }
            */

            customer.IsDeleted = true;
            customer.UpdatedAt = DateTime.UtcNow;
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
