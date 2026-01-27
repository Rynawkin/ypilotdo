using FluentValidation;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class CreateRouteCommandStartDetails
{
    public TimeSpan StartTime { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class CreateRouteStartDetailsValidator : AbstractValidator<CreateRouteCommandStartDetails>
{
    public CreateRouteStartDetailsValidator()
    {
        RuleFor(x => x.StartTime).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Address).NotEmpty();
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}