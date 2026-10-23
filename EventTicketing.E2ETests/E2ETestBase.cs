using Microsoft.Playwright.NUnit;

namespace EventTicketing.E2ETests;

public abstract class E2ETestBase : PageTest
{
    protected static string BaseUrl =>
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5173";

    // Full-stack tests hit a freshly started API, whose first request (JIT,
    // first DB connection, password hashing) can exceed Playwright's 5s default.
    [SetUp]
    public void UseGenerousTimeouts()
    {
        Page.SetDefaultTimeout(30_000);
        Page.SetDefaultNavigationTimeout(30_000);
        SetDefaultExpectTimeout(15_000);
    }

    /// <summary>
    /// Cleans up an event a test created, as the signed-in admin. One with orders can't be deleted, so it is
    /// archived instead, which takes it out of the shared dev database's list.
    /// </summary>
    protected async Task RemoveEventAsync(int id)
    {
        var deleted = await Page.APIRequest.DeleteAsync($"{BaseUrl}/api/v1/events/{id}");
        if (deleted.Status == 409)
            await Page.APIRequest.PostAsync($"{BaseUrl}/api/v1/events/{id}/archive");
    }
}
