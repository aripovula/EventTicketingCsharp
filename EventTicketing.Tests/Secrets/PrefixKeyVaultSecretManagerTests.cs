using Azure.Security.KeyVault.Secrets;
using EventTicketing.Api.Secrets;

namespace EventTicketing.Tests.Secrets;

public class PrefixKeyVaultSecretManagerTests
{
    private readonly PrefixKeyVaultSecretManager _manager = new("EventTicketing");

    private static SecretProperties Properties(string name) => SecretModelFactory.SecretProperties(name: name);

    [Fact]
    public void Load_TakesOnlyThisAppsSecrets()
    {
        Assert.True(_manager.Load(Properties("EventTicketing--Jwt--SigningKey")));
        Assert.False(_manager.Load(Properties("FinanceTracker--Jwt--SigningKey")));
        Assert.False(_manager.Load(Properties("EventTicketingJwt")));
    }

    [Fact]
    public void GetKey_DropsThePrefixAndMapsDashesToTheKeyDelimiter()
    {
        var secret = SecretModelFactory.KeyVaultSecret(Properties("EventTicketing--Stripe--SecretKey"), "sk_test_x");

        Assert.Equal("Stripe:SecretKey", _manager.GetKey(secret));
    }
}
