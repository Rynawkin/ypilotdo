using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Responses.Workspace;

namespace Monolith.WebAPI.Applications.Commands.Customers
{
    public static class CustomerMapper
    {
        public static CustomerResponse ToResponse(Customer customer)
        {
            if (customer == null)
                return null;

            return new CustomerResponse
            {
                Id = customer.Id,
                WorkspaceId = customer.WorkspaceId,
                Code = customer.Code,
                Name = customer.Name,
                Address = customer.Address,
                Phone = customer.Phone,
                Email = customer.Email,
                
                // WhatsApp Alanları - YENİ EKLENEN
                WhatsApp = customer.WhatsApp,
                WhatsAppOptIn = customer.WhatsAppOptIn,
                WhatsAppVerified = customer.WhatsAppVerified,
                WhatsAppOptInDate = customer.WhatsAppOptInDate,
                
                Latitude = customer.Latitude,
                Longitude = customer.Longitude,
                Priority = customer.Priority,
                EstimatedServiceTime = customer.EstimatedServiceTime,
                Notes = customer.Notes,
                Tags = !string.IsNullOrEmpty(customer.Tags) 
                    ? customer.Tags.Split(',').ToList() 
                    : new List<string>(),
                TimeWindow = (customer.TimeWindowStart.HasValue && customer.TimeWindowEnd.HasValue)
                    ? new TimeWindowDto
                    {
                        Start = customer.TimeWindowStart.Value.ToString(@"hh\:mm"),
                        End = customer.TimeWindowEnd.Value.ToString(@"hh\:mm")
                    }
                    : null,
                CreatedAt = customer.CreatedAt,
                UpdatedAt = customer.UpdatedAt ?? customer.CreatedAt,
                LastDeliveryDate = customer.LastDeliveryDate
            };
        }

        public static List<CustomerResponse> ToResponseList(IEnumerable<Customer> customers)
        {
            return customers?.Select(ToResponse).ToList() ?? new List<CustomerResponse>();
        }
    }
}