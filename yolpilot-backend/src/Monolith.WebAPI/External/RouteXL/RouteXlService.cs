using Flurl.Http;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.External.RouteXL.Models;

namespace Monolith.WebAPI.External.RouteXL;

public class RouteXlService(AppDbContext context)
{
    private const string Username = "halilkocaoz";
    private const string Password = "GaUskfc3@S5F$QF";

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly FlurlClient _tourClient = new("https://api.routexl.com/tour/");

    public async Task<RouteXlResponse> OptimizeAsync(List<RouteXlRequestLocation> locations, CancellationToken cancellationToken)
    {
        if (locations == null || locations.Count == 0)
            throw new ArgumentException("Locations cannot be null or empty", nameof(locations));

        await _lock.WaitAsync(cancellationToken);
        try
        {
            retry:
            var response = await _tourClient.Request()
                .AllowHttpStatus(429)
                .WithBasicAuth(Username, Password)
                .PostUrlEncodedAsync(new
                {
                    locations = System.Text.Json.JsonSerializer.Serialize(locations)
                }, cancellationToken: cancellationToken);

            if (response.StatusCode == 429)
                goto retry;

            return Newtonsoft.Json.JsonConvert.DeserializeObject<RouteXlResponse>(await response.GetStringAsync());
        }
        catch (Exception e)
        {
            var log = new Log(e);
            await context.Logs.AddAsync(log, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }
}