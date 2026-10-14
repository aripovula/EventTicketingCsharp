using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;

namespace EventTicketing.Api.Secrets;

/// <summary>
/// Loads only this app's secrets from a vault the trio apps share, and maps their names to
/// configuration keys: "EventTicketing--Jwt--SigningKey" becomes "Jwt:SigningKey".
/// </summary>
public class PrefixKeyVaultSecretManager(string prefix) : KeyVaultSecretManager
{
    private readonly string _prefix = $"{prefix}--";

    public override bool Load(SecretProperties secret) =>
        secret.Name.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase);

    // Secret names can't contain ':', so "--" stands for the configuration key delimiter.
    public override string GetKey(KeyVaultSecret secret) =>
        secret.Name[_prefix.Length..].Replace("--", ConfigurationPath.KeyDelimiter);
}
