using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.DeploymentStacks;
using Azure.ResourceManager.Resources.DeploymentStacks.Models;
using Microsoft.Extensions.Options;

namespace EventTicketing.DemoLauncher;

/// <summary>
/// The demo as a deployment stack in the demos resource group: the stack records every resource the
/// template created, so deleting it with "delete" removes exactly the demo and nothing else.
/// </summary>
public class AzureDemoStack(ArmClient arm, HttpClient http, IOptions<DemoOptions> options) : IDemoStack
{
    private DemoOptions Options => options.Value;

    private DeploymentStackCollection Stacks =>
        arm.GetDeploymentStacks(ResourceGroupResource.CreateResourceIdentifier(Options.SubscriptionId, Options.ResourceGroup));

    public async Task<StackInfo> GetAsync(CancellationToken cancellationToken)
    {
        var stack = await Stacks.GetIfExistsAsync(Options.StackName, cancellationToken);
        if (!stack.HasValue)
            return new StackInfo(StackState.None, null);

        var data = stack.Value!.Data;
        // The stack keeps the parameters it was created with, so it knows its own address.
        var url = data.Parameters.TryGetValue("dnsLabel", out var label) && label.Value is not null
            ? Options.UrlFor(label.Value.ToObjectFromJson<string>()!)
            : null;
        return new StackInfo(StateOf(data.ProvisioningState), url);
    }

    private static StackState StateOf(DeploymentStackProvisioningState? state)
    {
        if (state == DeploymentStackProvisioningState.Succeeded)
            return StackState.Deployed;
        if (state == DeploymentStackProvisioningState.Failed || state == DeploymentStackProvisioningState.Canceled)
            return StackState.Failed;
        if (state == DeploymentStackProvisioningState.Deleting || state == DeploymentStackProvisioningState.DeletingResources)
            return StackState.Deleting;
        return StackState.Deploying;
    }

    public async Task<string> StartCreateAsync(CancellationToken cancellationToken)
    {
        var dnsLabel = Options.NewDnsLabel();
        // The template comes from the same release as the app, so the two always match.
        var template = await http.GetStringAsync(Options.TemplateUrl, cancellationToken);
        var data = new DeploymentStackData
        {
            Description = "The Event Ticketing demo, managed by the demo launcher.",
            Template = BinaryData.FromString(template),
            ActionOnUnmanage = new ActionOnUnmanage(UnmanageActionResourceMode.Delete)
            {
                ResourceGroups = UnmanageActionResourceGroupMode.Delete,
            },
            DenySettings = new DeploymentStackDenySettings(DeploymentStackDenySettingsMode.None),
        };
        foreach (var (name, value) in Parameters(dnsLabel))
            data.Parameters[name] = new DeploymentParameterItem { Value = BinaryData.FromObjectAsJson(value) };

        try
        {
            await Stacks.CreateOrUpdateAsync(WaitUntil.Started, Options.StackName, data, cancellationToken);
        }
        catch (RequestFailedException e) when (e.Status == 409)
        {
            // Another request started it a moment ago.
        }
        return Options.UrlFor(dnsLabel);
    }

    public async Task StartDeleteAsync(CancellationToken cancellationToken)
    {
        var stack = await Stacks.GetIfExistsAsync(Options.StackName, cancellationToken);
        if (!stack.HasValue)
            return;

        try
        {
            await stack.Value!.DeleteAsync(WaitUntil.Started, UnmanageActionResourceMode.Delete,
                UnmanageActionResourceGroupMode.Delete, null, null, null, cancellationToken);
        }
        catch (RequestFailedException e) when (e.Status == 409)
        {
            // Already being deleted.
        }
    }

    private Dictionary<string, string> Parameters(string dnsLabel) => new()
    {
        ["dnsLabel"] = dnsLabel,
        ["releaseUrl"] = Options.ReleaseUrl,
        ["keyVaultUri"] = Options.KeyVaultUri,
        ["identityName"] = Options.IdentityName,
        ["identityResourceGroup"] = Options.IdentityResourceGroup,
        ["adminSshPublicKey"] = Options.AdminSshPublicKey,
        ["location"] = Options.Location,
    };
}
