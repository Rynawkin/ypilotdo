using FluentValidation;
using MediatR;
using Monolith.WebAPI.Extensions;

namespace Monolith.WebAPI.Applications.Behaviors;

public class ValidatorBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidatorBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var typeName = request.GetGenericTypeName();

        logger.LogInformation("Validating command {CommandType}", typeName);

        var failures = validators
            .Select(v => v.Validate(request))
            .SelectMany(result => result.Errors)
            .Where(error => error != null)
            .ToList();

        if (failures.Count != 0)
        {
            logger.LogWarning("Validation errors - {CommandType} - Command: {@Command} - Errors: {@ValidationErrors}", typeName, request,
                failures);
            throw new ValidationException("Validation exception", failures);
        }

        return await next();
    }
}