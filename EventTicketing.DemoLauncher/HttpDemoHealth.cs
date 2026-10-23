namespace EventTicketing.DemoLauncher;

/// <summary>Asks the demo's readiness endpoint over HTTPS, which also proves Caddy has its certificate.</summary>
public class HttpDemoHealth(HttpClient http) : IDemoHealth
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public async Task<bool> IsUpAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.GetAsync($"{url}/health/ready", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }
}
