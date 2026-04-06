using System.Security.Cryptography;
using System.Text;

namespace Monolith.WebAPI.Services.Marketing;

public static class MarketingTrackingHelpers
{
    public static string? HashIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return null;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ip.Trim()));
        return Convert.ToHexString(bytes);
    }
}
