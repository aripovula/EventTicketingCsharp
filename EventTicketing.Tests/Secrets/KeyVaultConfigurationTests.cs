using Azure.Identity;
using EventTicketing.Api.Secrets;
using Microsoft.Extensions.Configuration;

namespace EventTicketing.Tests.Secrets;

public class KeyVaultConfigurationTests
{
    // The builder only records sources; nothing contacts the vault until Build().
    private static bool HasKeyVault(IConfigurationBuilder builder) =>
        builder.Sources.Any(s => s.GetType().Name == "AzureKeyVaultConfigurationSource");

    [Fact]
    public void WithoutAVaultUri_AddsNothing()
    {
        var builder = new ConfigurationBuilder().AddKeyVaultIfConfigured(null);

        Assert.False(HasKeyVault(builder));
    }

    [Fact]
    public void WithAVaultUri_AddsTheVault()
    {
        var builder = new ConfigurationBuilder()
            .AddKeyVaultIfConfigured("https://example-vault.vault.azure.net/", new DefaultAzureCredential());

        Assert.True(HasKeyVault(builder));
    }
}
