using Microsoft.Extensions.Options;

namespace EventTicketing.DemoLauncher;

/// <summary>Asks the demo's readiness endpoint over HTTPS, which also proves Caddy has its certificate.</summary>
public class HttpDemoHealth(HttpClient http, IOptions<DemoOptions> options) : IDemoHealth
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public async Task<bool> IsUpAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.GetAsync($"{options.Value.Url}/health/ready", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }
}
