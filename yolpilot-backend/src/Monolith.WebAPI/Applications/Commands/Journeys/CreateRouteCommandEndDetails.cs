using FluentValidation;

namespace Monolith.WebAPI.Applications.Commands.Journeys;

public class CreateRouteCommandEndDetails
{
    public string Name { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class CreateRouteEndDetailsValidator : AbstractValidator<CreateRouteCommandEndDetails>
{
    public CreateRouteEndDetailsValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Address).NotEmpty();
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}