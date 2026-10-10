using EventTicketing.Api.Services;

namespace EventTicketing.Tests.Services;

public class SecureTokenTests
{
    [Fact]
    public void Generate_ReturnsAUrlSafeTokenThatDiffersEachTime()
    {
        var first = SecureToken.Generate();
        var second = SecureToken.Generate();

        Assert.Matches("^[A-Za-z0-9_-]{86}$", first);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Hash_ReturnsTheLowercaseSha256Hex()
    {
        Assert.Equal("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824", SecureToken.Hash("hello"));
    }
}
