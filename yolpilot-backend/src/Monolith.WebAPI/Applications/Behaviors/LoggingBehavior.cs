using MediatR;
using Monolith.WebAPI.Extensions;

namespace Monolith.WebAPI.Applications.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // ⭐ PERFORMANCE FIX: Only log in Debug mode to reduce overhead
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Handling command {CommandName}", request.GetGenericTypeName());
        }
        
        var response = await next();
        
        // ⭐ PERFORMANCE FIX: Simplified logging without object serialization
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Command {CommandName} completed", request.GetGenericTypeName());
        }

        return response;
    }
}