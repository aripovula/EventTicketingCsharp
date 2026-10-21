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

    public async Task<StackState> GetStateAsync(CancellationToken cancellationToken)
    {
        var stack = await Stacks.GetIfExistsAsync(Options.StackName, cancellationToken);
        if (!stack.HasValue)
            return StackState.None;

        var state = stack.Value!.Data.ProvisioningState;
        if (state == DeploymentStackProvisioningState.Succeeded)
            return StackState.Deployed;
        if (state == DeploymentStackProvisioningState.Failed || state == DeploymentStackProvisioningState.Canceled)
            return StackState.Failed;
        if (state == DeploymentStackProvisioningState.Deleting || state == DeploymentStackProvisioningState.DeletingResources)
            return StackState.Deleting;
        return StackState.Deploying;
    }

    public async Task StartCreateAsync(CancellationToken cancellationToken)
    {
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
        foreach (var (name, value) in Parameters())
            data.Parameters[name] = new DeploymentParameterItem { Value = BinaryData.FromObjectAsJson(value) };

        try
        {
            await Stacks.CreateOrUpdateAsync(WaitUntil.Started, Options.StackName, data, cancellationToken);
        }
        catch (RequestFailedException e) when (e.Status == 409)
        {
            // Another request started it a moment ago.
        }
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

    private Dictionary<string, string> Parameters() => new()
    {
        ["dnsLabel"] = Options.DnsLabel,
        ["releaseUrl"] = Options.ReleaseUrl,
        ["keyVaultUri"] = Options.KeyVaultUri,
        ["identityName"] = Options.IdentityName,
        ["identityResourceGroup"] = Options.IdentityResourceGroup,
        ["adminSshPublicKey"] = Options.AdminSshPublicKey,
        ["location"] = Options.Location,
    };
}
