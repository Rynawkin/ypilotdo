using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Applications.Commands;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands.Customers
{
    public class BulkImportCustomersCommand : BaseAuthenticatedCommand<BulkImportResponse>
    {
        [JsonIgnore] public override bool RequiresDispatcher => true;
        
        public List<CustomerImportDto> Customers { get; set; } = new List<CustomerImportDto>();
    }

    public class BulkImportCustomersCommandHandler : BaseAuthenticatedCommandHandler<BulkImportCustomersCommand, BulkImportResponse>
    {
        private readonly AppDbContext _context;

        public BulkImportCustomersCommandHandler(AppDbContext context, IUserService userService)
            : base(userService)
        {
            _context = context;
        }

        protected override async Task<BulkImportResponse> HandleCommand(BulkImportCustomersCommand request, CancellationToken cancellationToken)
        {
            var response = new BulkImportResponse
            {
                TotalCount = request.Customers.Count,
                Results = new List<ImportResult>()
            };

            // Get existing customer codes for duplicate check
            var existingCodes = await _context.Customers
                .Where(c => c.WorkspaceId == User.WorkspaceId && !c.IsDeleted)
                .Select(c => c.Code)
                .ToListAsync(cancellationToken);

            var customersToAdd = new List<Customer>();
            int rowIndex = 0;

            foreach (var dto in request.Customers)
            {
                rowIndex++;
                var result = new ImportResult
                {
                    RowIndex = rowIndex,
                    Name = dto.Name,
                    Code = dto.Code
                };

                try
                {
                    // Validate required fields
                    if (string.IsNullOrEmpty(dto.Name))
                    {
                        result.Success = false;
                        result.Error = "İsim zorunludur";
                        response.Results.Add(result);
                        response.FailureCount++;
                        continue;
                    }

                    // Generate code if not provided
                    if (string.IsNullOrEmpty(dto.Code))
                    {
                        var customerCount = existingCodes.Count + customersToAdd.Count;
                        dto.Code = $"MUS{(customerCount + 1).ToString("D3")}";
                    }
                    else if (existingCodes.Contains(dto.Code) || customersToAdd.Any(c => c.Code == dto.Code))
                    {
                        result.Success = false;
                        result.Error = $"Müşteri kodu zaten kullanılıyor: {dto.Code}";
                        response.Results.Add(result);
                        response.FailureCount++;
                        continue;
                    }

                    var customer = new Customer
                    {
                        WorkspaceId = User.WorkspaceId,
                        Code = dto.Code,
                        Name = dto.Name,
                        Address = dto.Address ?? string.Empty,
                        Phone = dto.Phone ?? string.Empty,
                        Email = dto.Email,
                        Latitude = dto.Latitude,
                        Longitude = dto.Longitude,
                        Priority = dto.Priority ?? "normal",
                        EstimatedServiceTime = dto.EstimatedServiceTime > 0 ? dto.EstimatedServiceTime : 10,
                        Notes = dto.Notes,
                        Tags = dto.Tags != null ? string.Join(",", dto.Tags) : null,
                        TimeWindowStart = ParseTimeWindow(dto.TimeWindow?.Start),
                        TimeWindowEnd = ParseTimeWindow(dto.TimeWindow?.End),
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    customersToAdd.Add(customer);
                    existingCodes.Add(customer.Code); // Add to list to prevent duplicates in same batch
                    
                    result.Success = true;
                    result.Code = customer.Code;
                    response.Results.Add(result);
                    response.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Error = $"Hata: {ex.Message}";
                    response.Results.Add(result);
                    response.FailureCount++;
                }
            }

            // Bulk insert successful customers
            if (customersToAdd.Any())
            {
                _context.Customers.AddRange(customersToAdd);
                await _context.SaveChangesAsync(cancellationToken);

                // Update results with created IDs
                for (int i = 0; i < customersToAdd.Count; i++)
                {
                    var successResult = response.Results.FirstOrDefault(r => r.Success && r.Code == customersToAdd[i].Code);
                    if (successResult != null)
                    {
                        successResult.CreatedId = customersToAdd[i].Id;
                    }
                }
            }

            response.Message = $"{response.SuccessCount} müşteri başarıyla eklendi, {response.FailureCount} başarısız.";
            return response;
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
