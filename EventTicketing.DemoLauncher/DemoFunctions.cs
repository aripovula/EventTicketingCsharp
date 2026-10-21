using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace EventTicketing.DemoLauncher;

public class DemoFunctions(DemoLifecycle demo)
{
    /// <summary>The portfolio page calls this when it opens: start the demo, or keep it up longer.</summary>
    [Function("RequestDemo")]
    public async Task<IActionResult> Request(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "demo")] HttpRequest request,
        CancellationToken cancellationToken) =>
        new OkObjectResult(await demo.RequestAsync(cancellationToken));

    /// <summary>The page polls this until the demo is ready; it changes nothing.</summary>
    [Function("GetDemo")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "demo")] HttpRequest request,
        CancellationToken cancellationToken) =>
        new OkObjectResult(await demo.GetStatusAsync(cancellationToken));

    [Function("ReconcileDemo")]
    public Task Reconcile([TimerTrigger("0 */2 * * * *")] TimerInfo timer, CancellationToken cancellationToken) =>
        demo.ReconcileAsync(cancellationToken);
}
