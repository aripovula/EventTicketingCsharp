using EventTicketing.Api.Filters;
using EventTicketing.Tests.Integration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.Tests.Filters;

// Runs the filter against the real IdempotencyService and a throwaway database.
[Trait("Category", "Integration")]
public class IdempotentAttributeTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private int _actionRuns;

    private async Task<IActionResult?> RunAsync(string? key, string path = "/api/v1/test")
    {
        var http = new DefaultHttpContext { RequestServices = factory.Services.CreateScope().ServiceProvider };
        http.Request.Path = path;
        if (key is not null)
            http.Request.Headers[IdempotentAttribute.HeaderName] = key;
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var executing = new ActionExecutingContext(actionContext, [], new Dictionary<string, object?>(), controller: new object());

        await new IdempotentAttribute().OnActionExecutionAsync(executing, () =>
        {
            _actionRuns++;
            return Task.FromResult(new ActionExecutedContext(actionContext, [], new object())
            {
                Result = new ObjectResult(new { orderId = 40 + _actionRuns }) { StatusCode = StatusCodes.Status201Created },
            });
        });
        return executing.Result;
    }

    [Fact]
    public async Task WithoutAKey_TheActionAlwaysRuns()
    {
        await RunAsync(null);
        await RunAsync(null);

        Assert.Equal(2, _actionRuns);
    }

    [Fact]
    public async Task ARepeatedKey_ReplaysTheFirstEnvelopedResponseWithoutRunningTheAction()
    {
        var key = Guid.NewGuid().ToString();
        await RunAsync(key);

        var replay = Assert.IsType<ContentResult>(await RunAsync(key));

        Assert.Equal(1, _actionRuns);
        Assert.Equal(StatusCodes.Status201Created, replay.StatusCode);
        Assert.Equal("application/json", replay.ContentType);
        Assert.Contains("\"data\": {\"orderId\": 41}", replay.Content);
        Assert.Contains("\"errors\": []", replay.Content);
    }

    [Fact]
    public async Task DifferentKeys_EachRunTheAction()
    {
        await RunAsync(Guid.NewGuid().ToString());
        await RunAsync(Guid.NewGuid().ToString());

        Assert.Equal(2, _actionRuns);
    }

    [Fact]
    public async Task TheSameKeyOnAnotherPath_RunsTheAction()
    {
        var key = Guid.NewGuid().ToString();
        await RunAsync(key, "/api/v1/one");
        await RunAsync(key, "/api/v1/two");

        Assert.Equal(2, _actionRuns);
    }

    [Fact]
    public async Task AKeyOver128Characters_IsRejectedWithoutRunningTheAction()
    {
        var result = Assert.IsType<BadRequestObjectResult>(await RunAsync(new string('k', 129)));

        Assert.Equal(0, _actionRuns);
        Assert.Contains("invalid_idempotency_key", System.Text.Json.JsonSerializer.Serialize(result.Value));
    }
}
