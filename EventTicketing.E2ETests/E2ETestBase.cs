using Microsoft.Playwright.NUnit;

namespace EventTicketing.E2ETests;

public abstract class E2ETestBase : PageTest
{
    protected static string BaseUrl =>
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5173";
}
