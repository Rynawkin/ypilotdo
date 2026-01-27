using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Applications.Commands.Customers;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Responses.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Queries.Customers
{
    public class GetCustomerQuery : BaseAuthenticatedCommand<CustomerResponse?>
    {
        public int Id { get; set; }
    }

    public class GetCustomerQueryHandler : BaseAuthenticatedCommandHandler<GetCustomerQuery, CustomerResponse?>
    {
        private readonly AppDbContext _context;

        public GetCustomerQueryHandler(AppDbContext context, IUserService userService)
            : base(userService)
        {
            _context = context;
        }

        protected override async Task<CustomerResponse?> HandleCommand(GetCustomerQuery request, CancellationToken cancellationToken)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == request.Id && c.WorkspaceId == User.WorkspaceId, cancellationToken);

            if (customer == null)
            {
                return null;
            }

            return CustomerMapper.ToResponse(customer);
        }
    }
}