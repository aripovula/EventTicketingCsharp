using System.Net;
using System.Web;
using Azure.Storage.Blobs;
using EventTicketing.Api.Storage;
using EventTicketing.Tests.Integration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventTicketing.Tests.Storage;

// Runs against Azurite: the SAS rules are only worth testing on a real Blob endpoint.
[Trait("Category", "Integration")]
public class BlobImageStorageTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];

    private static readonly HttpClient Http = new();

    private BlobImageStorage Storage =>
        new(factory.Services.GetRequiredService<BlobServiceClient>(), factory.Services.GetRequiredService<IOptions<BlobStorageOptions>>());

    private static Task<HttpResponseMessage> PutAsync(string url, byte[] body, string contentType = "image/png")
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = new ByteArrayContent(body) };
        request.Headers.Add("x-ms-blob-type", "BlockBlob");
        request.Headers.Add("x-ms-blob-content-type", contentType);
        return Http.SendAsync(request, TestContext.Current.CancellationToken);
    }

    [Fact]
    public void CreateUploadUrl_IsWriteOnlyAndExpiresInFiveMinutes()
    {
        var slot = Storage.CreateUploadUrl("image/png");

        var query = HttpUtility.ParseQueryString(new Uri(slot.UploadUrl).Query);
        Assert.Equal("cw", query["sp"]);
        Assert.InRange(slot.ExpiresAt - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(4.9), TimeSpan.FromMinutes(5));
        Assert.EndsWith(".png", slot.ImageUrl);
        Assert.StartsWith(slot.ImageUrl + "?", slot.UploadUrl);
    }

    [Fact]
    public void CreateUploadUrl_NamesEveryBlobFreshly()
    {
        Assert.NotEqual(Storage.CreateUploadUrl("image/jpeg").ImageUrl, Storage.CreateUploadUrl("image/jpeg").ImageUrl);
    }

    [Fact]
    public async Task TheBrowserUploadsWithTheUrl_AndAnyoneCanReadTheImage()
    {
        var slot = Storage.CreateUploadUrl("image/png");

        var put = await PutAsync(slot.UploadUrl, Png);
        var image = await Http.GetAsync(slot.ImageUrl, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, put.StatusCode);
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Png, await image.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TheUploadUrl_CannotDeleteTheImage()
    {
        var slot = Storage.CreateUploadUrl("image/png");
        await PutAsync(slot.UploadUrl, Png);

        var delete = await Http.DeleteAsync(slot.UploadUrl, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Http.GetAsync(slot.ImageUrl, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task NobodyCanListTheImages()
    {
        var slot = Storage.CreateUploadUrl("image/png");
        await PutAsync(slot.UploadUrl, Png);
        var container = slot.ImageUrl[..slot.ImageUrl.LastIndexOf('/')];

        var list = await Http.GetAsync($"{container}?restype=container&comp=list", TestContext.Current.CancellationToken);

        Assert.False(list.IsSuccessStatusCode);
    }

    [Fact]
    public async Task TheConfiguredOrigin_MayPutFromTheBrowser()
    {
        // The preflight a browser sends before its cross-origin PUT.
        var slot = Storage.CreateUploadUrl("image/png");
        var preflight = new HttpRequestMessage(HttpMethod.Options, slot.UploadUrl);
        preflight.Headers.Add("Origin", "http://localhost:5173");
        preflight.Headers.Add("Access-Control-Request-Method", "PUT");
        preflight.Headers.Add("Access-Control-Request-Headers", "x-ms-blob-type,x-ms-blob-content-type");

        var response = await Http.SendAsync(preflight, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("http://localhost:5173", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}
