using System.ComponentModel.DataAnnotations;

namespace EventTicketing.DemoLauncher;

/// <summary>
/// Where the one shared demo lives and how to deploy it. infra/launcher.bicep sets these as
/// <c>Demo__*</c> app settings.
/// </summary>
public class DemoOptions
{
    public const string Section = "Demo";

    [Required] public string SubscriptionId { get; set; } = "";

    /// <summary>The permanent resource group the launcher may deploy into (and nowhere else).</summary>
    [Required] public string ResourceGroup { get; set; } = "";

    /// <summary>Name of the deployment stack that holds every resource of the demo.</summary>
    [Required] public string StackName { get; set; } = "";

    [Required] public string Location { get; set; } = "";

    /// <summary>The demo is served at https://&lt;label&gt;.&lt;location&gt;.cloudapp.azure.com.</summary>
    [Required] public string DnsLabel { get; set; } = "";

    /// <summary>The compiled infra/demo-stack.bicep, as attached to the GitHub release.</summary>
    [Required, Url] public string TemplateUrl { get; set; } = "";

    /// <summary>The release tarball the demo VM downloads.</summary>
    [Required, Url] public string ReleaseUrl { get; set; } = "";

    [Required, Url] public string KeyVaultUri { get; set; } = "";

    [Required] public string IdentityName { get; set; } = "";

    [Required] public string IdentityResourceGroup { get; set; } = "";

    [Required] public string AdminSshPublicKey { get; set; } = "";

    /// <summary>The table service holding the demo's expiry time.</summary>
    [Required, Url] public string TableServiceUri { get; set; } = "";

    /// <summary>How long the demo stays up after the last request.</summary>
    [Range(1, 240)] public int LifetimeMinutes { get; set; } = 30;

    public string UrlFor(string dnsLabel) => $"https://{dnsLabel}.{Location}.cloudapp.azure.com";
}
