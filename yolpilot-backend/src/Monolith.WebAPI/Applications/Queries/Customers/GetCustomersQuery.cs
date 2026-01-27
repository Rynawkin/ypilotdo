using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Applications.Commands.Customers;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Customers
{
    public class GetCustomersQuery : BaseAuthenticatedCommand<List<CustomerResponse>>
    {
        public string? SearchTerm { get; set; }
        public string? Priority { get; set; }
        public List<string>? Tags { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        
        
    }

    public class GetCustomersQueryHandler : BaseAuthenticatedCommandHandler<GetCustomersQuery, List<CustomerResponse>>
    {
        private readonly AppDbContext _context;

        public GetCustomersQueryHandler(AppDbContext context, IUserService userService)
            : base(userService)
        {
            _context = context;
        }

        protected override async Task<List<CustomerResponse>> HandleCommand(GetCustomersQuery request, CancellationToken cancellationToken)
        {
            // ✅ Driver bu noktaya gelemez çünkü RequiresDispatcher = true
            
            // Base query - sadece bu workspace'e ait müşteriler
            var query = _context.Customers
                .Where(c => c.WorkspaceId == User.WorkspaceId)
                .AsQueryable();

            // IsDeleted alanı varsa kontrol et
            if (_context.Model.FindEntityType(typeof(Data.Workspace.Customer))?.FindProperty("IsDeleted") != null)
            {
                query = query.Where(c => !c.IsDeleted);
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(c => 
                    c.Name.ToLower().Contains(searchTerm) ||
                    c.Code.ToLower().Contains(searchTerm) ||
                    c.Address.ToLower().Contains(searchTerm) ||
                    c.Phone.Contains(searchTerm) ||
                    (c.Email != null && c.Email.ToLower().Contains(searchTerm))
                );
            }

            // Priority filter
            if (!string.IsNullOrWhiteSpace(request.Priority))
            {
                query = query.Where(c => c.Priority == request.Priority);
            }

            // Tags filter
            if (request.Tags != null && request.Tags.Any())
            {
                foreach (var tag in request.Tags)
                {
                    query = query.Where(c => c.Tags != null && c.Tags.Contains(tag));
                }
            }

            // Sıralama - son güncellenenler önce
            query = query.OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
                        .ThenByDescending(c => c.CreatedAt);

            // PERFORMANCE: Always use pagination to prevent memory exhaustion
            var pageNumber = request.PageNumber ?? 1;
            var pageSize = request.PageSize ?? 50; // Default 50 items per page
            var skip = (pageNumber - 1) * pageSize;
            query = query.Skip(skip).Take(pageSize);

            var customers = await query.ToListAsync(cancellationToken);

            // Response oluştur
            return CustomerMapper.ToResponseList(customers);
        }
    }
}