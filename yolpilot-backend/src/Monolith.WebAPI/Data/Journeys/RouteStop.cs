using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using Monolith.WebAPI.Applications.Commands.Journeys;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.Requests;

namespace Monolith.WebAPI.Data.Journeys;

public class RouteStop : BaseEntity
{
    // do not remove this constructor
    public RouteStop()
    {
    }

    public RouteStop(RouteStopRequest command, int routeId, TimeSpan defaultServiceTime)
    {
        CustomerId = command.CustomerId;
        Name = command.Name;
        Address = command.Address;
        Latitude = command.Latitude;
        Longitude = command.Longitude;
        Notes = command.Notes;
        ContactFullName = command.ContactFullName;
        ContactPhone = command.ContactPhone;
        ContactEmail = command.ContactEmail;
        Type = command.Type;
        OrderType = command.OrderType;
        ProofOfDeliveryRequired = command.ProofOfDeliveryRequired;
        SignatureRequired = command.SignatureRequired ?? false;
        PhotoRequired = command.PhotoRequired ?? false;
        ArriveBetweenStart = command.ArriveBetweenStart;
        ArriveBetweenEnd = command.ArriveBetweenEnd;
        ServiceTime = command.ServiceTime ?? defaultServiceTime;
        RouteId = routeId;
        
        // Order'ı burada set etmiyoruz, Route constructor'ında set edilecek
    }

    public void Update(UpdateStopCommand command)
    {
        CustomerId = command.CustomerId;
        Name = command.Name;
        Address = command.Address;
        Latitude = command.Latitude;
        Longitude = command.Longitude;
        Notes = command.Notes;
        ContactFullName = command.ContactFullName;
        ContactPhone = command.ContactPhone;
        ContactEmail = command.ContactEmail;
        Type = command.Type;
        OrderType = command.OrderType;
        ProofOfDeliveryRequired = command.ProofOfDeliveryRequired;
        SignatureRequired = command.SignatureRequired ?? SignatureRequired;
        PhotoRequired = command.PhotoRequired ?? PhotoRequired;
        ArriveBetweenStart = command.ArriveBetweenStart;
        ArriveBetweenEnd = command.ArriveBetweenEnd;
        ServiceTime = command.ServiceTime;

        UpdatedAt = DateTime.UtcNow;
    }

    // Customer relationship
    public int? CustomerId { get; set; }
    public Customer Customer { get; set; }

    [MaxLength(128)] public string Name { get; private set; }
    [MaxLength(1024)] public string Address { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    [MaxLength(1024)] public string Notes { get; private set; }

    [MaxLength(128)] public string ContactFullName { get; private set; }
    [MaxLength(32)] public string ContactPhone { get; private set; }
    [MaxLength(64)] public string ContactEmail { get; private set; }

    public LocationType Type { get; private set; }
    public OrderType OrderType { get; private set; }
    public bool ProofOfDeliveryRequired { get; private set; }
    public bool SignatureRequired { get; set; } = false;
    public bool PhotoRequired { get; set; } = false;

    [Column(TypeName = "time")]
    public TimeSpan? ArriveBetweenStart { get; set; }

    [Column(TypeName = "time")]
    public TimeSpan? ArriveBetweenEnd { get; set; }

    public TimeSpan? ServiceTime { get; private set; }
    
    public int Order { get; set; }
    
    // ETA field'ları WITH COLUMN ATTRIBUTES
    [Column(TypeName = "time")]
    public TimeSpan? EstimatedArrivalTime { get; set; }
    
    [Column(TypeName = "time")]
    public TimeSpan? EstimatedDepartureTime { get; set; }

    // Soft exclude fields for mobile deviation optimization
    public bool IsExcluded { get; set; } = false;
    
    [MaxLength(256)]
    public string? ExclusionReason { get; set; }

    public int RouteId { get; private set; }
    public Route Route { get; set; }

    [NotMapped]
    public string LatLng => $"{Latitude.ToString(CultureInfo.InvariantCulture)},{Longitude.ToString(CultureInfo.InvariantCulture)}";
}

public enum LocationType
{
    Delivery = 10,
    Pickup = 20,
}

public enum OrderType
{
    First = 10,
    Auto = 20,
    Last = 30
}