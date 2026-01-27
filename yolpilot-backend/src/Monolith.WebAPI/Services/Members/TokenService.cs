using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Responses.Members;

namespace Monolith.WebAPI.Services.Members;

public class TokenModel
{
    public string BearerToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresIn { get; set; }
    public UserResponse Me { get; set; }
}

public interface ITokenService
{
    ClaimsPrincipal GetClaimsPrincipal(string bearerToken);
    TokenModel Create(ApplicationUser user);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly SymmetricSecurityKey _jwtKey;
    private const string SecurityAlgorithm = SecurityAlgorithms.HmacSha256;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        _jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? throw new ApiException("Jwt:Key missing")));
    }

    public ClaimsPrincipal GetClaimsPrincipal(string bearerToken)
    {
        // SECURITY: Full JWT validation with issuer, audience, and lifetime checks
        var parms = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = _configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _jwtKey,
            ClockSkew = TimeSpan.Zero
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(bearerToken, parms, out var stoken);
        var valid = stoken is JwtSecurityToken jwt &&
                    jwt.Header.Alg.Equals(SecurityAlgorithm, StringComparison.InvariantCultureIgnoreCase);
        if (!valid) throw new ApiException("Invalid token", 400);
        return principal;
    }

    public TokenModel Create(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullName ?? string.Empty),

            // GERİ UYUMLULUK: iki ad birden
            new("WorkspaceId", user.WorkspaceId.ToString()),
            new("workspaceId", user.WorkspaceId.ToString()),
        };

        if (user.IsAdmin)      claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        if (user.IsDispatcher) claims.Add(new Claim(ClaimTypes.Role, "Dispatcher"));
        if (user.IsDriver)     claims.Add(new Claim(ClaimTypes.Role, "Driver"));
        if (user.IsSuperAdmin) claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));

        var expires = DateTime.UtcNow.AddDays(7);
        var creds = new SigningCredentials(_jwtKey, SecurityAlgorithm);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new TokenModel
        {
            BearerToken = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = Guid.NewGuid().ToString(),
            ExpiresIn = expires,
            Me = new UserResponse(user)
        };
    }
}