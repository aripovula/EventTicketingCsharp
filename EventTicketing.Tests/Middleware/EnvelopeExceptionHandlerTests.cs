using System.Text.Json;
using EventTicketing.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventTicketing.Tests.Middleware;

public class EnvelopeExceptionHandlerTests
{
    private static EnvelopeExceptionHandler Build() =>
        new(NullLogger<EnvelopeExceptionHandler>.Instance);

    private static DefaultHttpContext BuildContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonElement> ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
    }

    [Fact]
    public async Task MarksExceptionHandled()
    {
        var handled = await Build().TryHandleAsync(
            BuildContext(), new InvalidOperationException("boom"), TestContext.Current.CancellationToken);

        Assert.True(handled);
    }

    [Fact]
    public async Task Returns500()
    {
        var context = BuildContext();

        await Build().TryHandleAsync(context, new InvalidOperationException("boom"), TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task WritesInternalErrorEnvelopeWithoutExceptionDetails()
    {
        var context = BuildContext();

        await Build().TryHandleAsync(context, new InvalidOperationException("secret detail"), TestContext.Current.CancellationToken);

        var body = await ReadBody(context);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("data").ValueKind);
        var error = Assert.Single(body.GetProperty("errors").EnumerateArray());
        Assert.Equal("internal_error", error.GetProperty("code").GetString());
        Assert.DoesNotContain("secret detail", body.GetRawText());
    }
}
