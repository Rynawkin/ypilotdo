using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Monolith.WebAPI.Services.Marketing;

public static class MarketingTrackingHelpers
{
    public sealed record GeoContext(
        string? IpAddress,
        string? CountryCode,
        string? CountryName,
        string? Region,
        string? City
    );

    public static string? HashIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return null;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ip.Trim()));
        return Convert.ToHexString(bytes);
    }

    public static GeoContext GetGeoContext(HttpRequest request, string? fallbackIp)
    {
        var ipAddress = GetClientIpAddress(request, fallbackIp);
        var countryCode = FirstHeader(request, "x-vercel-ip-country", "cf-ipcountry");
        var countryName = FirstHeader(request, "x-country-name");
        var region = FirstHeader(request, "x-vercel-ip-country-region", "x-region-name");
        var city = FirstHeader(request, "x-vercel-ip-city", "x-city-name");

        if (string.IsNullOrWhiteSpace(countryName) && !string.IsNullOrWhiteSpace(countryCode))
        {
            countryName = countryCode;
        }

        return new GeoContext(
            IpAddress: ipAddress,
            CountryCode: countryCode,
            CountryName: countryName,
            Region: region,
            City: city
        );
    }

    private static string? GetClientIpAddress(HttpRequest request, string? fallbackIp)
    {
        var forwarded = FirstHeader(request, "x-forwarded-for", "x-real-ip", "cf-connecting-ip", "x-client-ip");
        var ip = forwarded?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();

        if (string.IsNullOrWhiteSpace(ip))
        {
            ip = fallbackIp;
        }

        return string.IsNullOrWhiteSpace(ip) ? null : ip.Trim();
    }

    private static string? FirstHeader(HttpRequest request, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (request.Headers.TryGetValue(key, out var value))
            {
                var raw = value.ToString();
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    return raw.Trim();
                }
            }
        }

        return null;
    }
}
