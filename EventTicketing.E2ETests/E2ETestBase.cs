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
}
