using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Applications.Commands.Customers;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Customers
{
    public class SearchCustomersQuery : BaseAuthenticatedCommand<List<CustomerResponse>>
    {
        public string SearchTerm { get; set; } = string.Empty;
    }

    public class SearchCustomersQueryHandler : BaseAuthenticatedCommandHandler<SearchCustomersQuery, List<CustomerResponse>>
    {
        private readonly AppDbContext _context;

        public SearchCustomersQueryHandler(AppDbContext context, IUserService userService)
            : base(userService)
        {
            _context = context;
        }

        protected override async Task<List<CustomerResponse>> HandleCommand(SearchCustomersQuery request, CancellationToken cancellationToken)
        {
            var searchTerm = request.SearchTerm.ToLower();

            var customers = await _context.Customers
                .Where(c => c.WorkspaceId == User.WorkspaceId &&
                    (c.Name.ToLower().Contains(searchTerm) ||
                     c.Code.ToLower().Contains(searchTerm) ||
                     c.Address.ToLower().Contains(searchTerm) ||
                     c.Phone.ToLower().Contains(searchTerm) ||
                     (c.Email != null && c.Email.ToLower().Contains(searchTerm))))
                .OrderBy(c => c.Name)
                .Take(50) // Maksimum 50 sonuç döndür
                .ToListAsync(cancellationToken);

            return CustomerMapper.ToResponseList(customers);
        }
    }
}