namespace EventTicketing.DemoLauncher.Tests;

public class DemoOptionsTests
{
    private static readonly DemoOptions Options = new() { DnsLabel = "eventticketing-demo", Location = "eastus" };

    [Fact]
    public void ANewDnsLabel_AddsFiveRandomCharactersToThePrefix()
    {
        Assert.Matches("^eventticketing-demo-[a-z0-9]{5}$", Options.NewDnsLabel());
    }

    [Fact]
    public void EachDemo_GetsADifferentDnsLabel()
    {
        var labels = Enumerable.Range(0, 20).Select(_ => Options.NewDnsLabel()).ToHashSet();

        Assert.Equal(20, labels.Count);
    }

    [Fact]
    public void TheUrl_IsTheLabelUnderTheRegionsCloudappDomain()
    {
        Assert.Equal("https://eventticketing-demo-x7k2q.eastus.cloudapp.azure.com", Options.UrlFor("eventticketing-demo-x7k2q"));
    }
}
