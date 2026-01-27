using System.Text.Json.Serialization;
using MediatR;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Services.Members;

namespace Monolith.WebAPI.Applications.Commands;

public abstract class BaseAuthenticatedCommand<TResponse> : IRequest<TResponse>
{
    [JsonIgnore] public Guid AuthenticatedUserId { get; set; }

    // ✅ YENİ: Daha spesifik yetkilendirme seçenekleri
    [JsonIgnore] public virtual bool RequiresSuperAdmin => false;
    [JsonIgnore] public virtual bool RequiresAdmin => false;
    [JsonIgnore] public virtual bool RequiresDispatcher => false;
    [JsonIgnore] public virtual bool RequiresDriver => false;
    [JsonIgnore] public virtual bool RequiresWorkspaceAccess => false; // Workspace kaynaklarına erişim için
}

public abstract class BaseAuthenticatedCommandHandler<TRequest, TResponse>(IUserService userService)
    : IRequestHandler<TRequest, TResponse>
    where TRequest : BaseAuthenticatedCommand<TResponse>
{
    protected readonly IUserService UserService = userService;

    /// <summary>
    /// Authenticated user instance.
    /// </summary>
    protected ApplicationUser User { get; private set; }

    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    {
        User = await UserService.GetUserAsync(request.AuthenticatedUserId, cancellationToken);

        // ✅ YENİ: Daha esnek yetkilendirme kontrolü
        if (request.RequiresSuperAdmin)
        {
            User.ThrowIfNotSuperAdmin();
        }
        else if (request.RequiresAdmin)
        {
            User.ThrowIfNotAdmin();
        }
        else if (request.RequiresDispatcher)
        {
            User.ThrowIfNotDispatcher();
        }
        else if (request.RequiresDriver)
        {
            User.ThrowIfNotDriver();
        }
        else if (request.RequiresWorkspaceAccess)
        {
            User.ThrowIfCannotAccessWorkspaceResources();
        }
        // Hiçbir özel yetki belirtilmemişse, sadece authenticated olması yeterli

        return await HandleCommand(request, cancellationToken);
    }

    protected abstract Task<TResponse> HandleCommand(TRequest request, CancellationToken cancellationToken);
}