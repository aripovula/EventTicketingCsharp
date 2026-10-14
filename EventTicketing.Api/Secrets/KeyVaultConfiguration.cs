using Azure.Core;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;

namespace EventTicketing.Api.Secrets;

public static class KeyVaultConfiguration
{
    public const string UriKey = "KeyVault:Uri";

    /// <summary>The prefix of this app's secret names in the shared vault.</summary>
    public const string SecretPrefix = "EventTicketing";

    /// <summary>
    /// Adds the vault named by <c>KeyVault:Uri</c>, if there is one (only deployed environments set it;
    /// locally secrets come from user-secrets). On the Azure VM, DefaultAzureCredential signs in with its
    /// managed identity; <c>AZURE_CLIENT_ID</c> picks the user-assigned one.
    /// </summary>
    public static IConfigurationBuilder AddKeyVaultIfConfigured(
        this IConfigurationBuilder builder, string? vaultUri, TokenCredential? credential = null)
    {
        if (string.IsNullOrEmpty(vaultUri))
            return builder;

        return builder.AddAzureKeyVault(new Uri(vaultUri), credential ?? new DefaultAzureCredential(),
            new AzureKeyVaultConfigurationOptions { Manager = new PrefixKeyVaultSecretManager(SecretPrefix) });
    }
}
