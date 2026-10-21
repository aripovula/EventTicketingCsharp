using Azure.Data.Tables;
using Azure.Identity;
using Azure.ResourceManager;
using EventTicketing.DemoLauncher;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

builder.Services.AddOptions<DemoOptions>()
    .Bind(builder.Configuration.GetSection(DemoOptions.Section))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// In Azure this is the function app's managed identity: there is no key or secret anywhere.
var credential = new DefaultAzureCredential();
builder.Services.AddSingleton(new ArmClient(credential));
builder.Services.AddSingleton(sp => new TableClient(
    new Uri(sp.GetRequiredService<IOptions<DemoOptions>>().Value.TableServiceUri), TableExpiryStore.TableName, credential));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IExpiryStore, TableExpiryStore>();
builder.Services.AddHttpClient<IDemoStack, AzureDemoStack>();
builder.Services.AddHttpClient<IDemoHealth, HttpDemoHealth>(http => http.Timeout = HttpDemoHealth.Timeout);
builder.Services.AddScoped<DemoLifecycle>();

builder.Build().Run();
