using FluentValidation;
using Monolith.WebAPI.Data.Journeys;

namespace Monolith.WebAPI.Requests;

public class RouteStopRequest
{
    public int? CustomerId { get; set; } // ✅ EKLENDI - CustomerId field'ı
    public string Name { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public string Notes { get; set; }

    public string ContactFullName { get; set; }
    public string ContactPhone { get; set; }
    public string ContactEmail { get; set; }

    public LocationType Type { get; set; }
    public OrderType OrderType { get; set; }
    public bool ProofOfDeliveryRequired { get; set; }
    public bool? SignatureRequired { get; set; }
    public bool? PhotoRequired { get; set; }

    public TimeSpan? ArriveBetweenStart { get; set; }
    public TimeSpan? ArriveBetweenEnd { get; set; }
    public TimeSpan? ServiceTime { get; set; }
    public TimeSpan? EstimatedArrivalTime { get; set; }
    public TimeSpan? EstimatedDepartureTime { get; set; }
}

public class StopRequestModelValidator : AbstractValidator<RouteStopRequest>
{
    public StopRequestModelValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).When(x => x.CustomerId.HasValue); // ✅ EKLENDI
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(1024);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.Notes).MaximumLength(1024);
        RuleFor(x => x.ContactFullName).MaximumLength(128);
        RuleFor(x => x.ContactPhone).MaximumLength(32);
        RuleFor(x => x.ContactEmail).MaximumLength(64);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.OrderType).IsInEnum();
        RuleFor(x => x.ArriveBetweenStart)
            .LessThan(x => x.ArriveBetweenEnd)
            .When(x => x.ArriveBetweenStart.HasValue && x.ArriveBetweenEnd.HasValue);
    }
}
