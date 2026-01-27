using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Members;

namespace Monolith.WebAPI.Services.Members;

public interface IRefreshTokenService
{
    /// <summary>
    /// Creates new refresh token.
    /// </summary>
    Task AddAsync(RefreshToken refreshToken);

    /// <summary>
    /// Gets the refresh token.
    /// </summary>
    Task<RefreshToken> GetAsync(Guid userId, string refreshTokenValue);

    /// <summary>
    /// Gets the refresh token by bearer token.
    /// </summary>
    Task<RefreshToken> GetAsyncByBearerToken(Guid userId, string bearerToken);

    /// <summary>
    /// Deletes the refresh token.
    /// </summary>
    public Task DeleteAsync(RefreshToken refreshToken);

    /// <summary>
    /// Saves the changes.
    /// </summary>
    Task<int> SaveChangesAsync();
}

public class RefreshTokenService(AppDbContext context) : IRefreshTokenService
{
    public async Task AddAsync(RefreshToken refreshToken)
        => await context.RefreshTokens.AddAsync(refreshToken);

    public async Task<RefreshToken> GetAsyncByBearerToken(Guid userId, string bearerToken)
        => await context.RefreshTokens.FirstOrDefaultAsync(x => x.UserId == userId.ToString() && x.BearerToken == bearerToken);

    public async Task<RefreshToken> GetAsync(Guid userId, string refreshTokenValue)
        => await context.RefreshTokens.FirstOrDefaultAsync(x => x.UserId == userId.ToString() && x.Token == refreshTokenValue);

    public Task DeleteAsync(RefreshToken refreshToken)
    {
        if (refreshToken is not null)
            context.Remove(refreshToken);

        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync()
        => await context.SaveChangesAsync();
}