using System.Net;
using ApricotFramework.AspNetCore.Extensions.Forwarding;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApricotFramework.AspNetCore.Extensions.Tests.Forwarding;

public class ForwardingExtensionsTests
{
    /// <summary>
    /// Registers the forwarding configuration and reads the resulting options back.
    /// </summary>
    /// <param name="configure">Registers the configuration under test.</param>
    /// <returns>The resolved options.</returns>
    private static ForwardedHeadersOptions GetOptions(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();

        configure(services);

        using var provider = services.BuildServiceProvider(validateScopes: true);

        return provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
    }

    [Fact]
    public void AddForwardingConfiguration_Always_HonoursForwardedForProtoAndHost()
    {
        var options = GetOptions(services => services.AddForwardingConfiguration());

        Assert.Equal(
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
            options.ForwardedHeaders);
    }

    [Fact]
    public void AddForwardingConfiguration_Always_DoesNotHonourForwardedPrefix()
    {
        var options = GetOptions(services => services.AddForwardingConfiguration());

        Assert.False(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedPrefix));
    }

    [Fact]
    public void AddForwardingConfiguration_Always_ClearsKnownNetworks()
    {
        var options = GetOptions(services => services.AddForwardingConfiguration());

        Assert.Empty(options.KnownIPNetworks);
    }

    [Fact]
    public void AddForwardingConfiguration_Always_ClearsKnownProxies()
    {
        var options = GetOptions(services => services.AddForwardingConfiguration());

        Assert.Empty(options.KnownProxies);
    }

    [Fact]
    public void AddForwardingConfiguration_WithCallback_RunsItAfterTheListsAreCleared()
    {
        // This ordering is the point of the overload: a caller can narrow the trust back down,
        // which is impossible if the clear happens last.
        var proxy = IPAddress.Parse("10.0.0.1");

        var options = GetOptions(services => services.AddForwardingConfiguration(o => o.KnownProxies.Add(proxy)));

        Assert.Equal([proxy], options.KnownProxies);
    }

    [Fact]
    public void AddForwardingConfiguration_WithCallback_CanRestoreAKnownNetwork()
    {
        // Qualified because Microsoft.AspNetCore.HttpOverrides has an IPNetwork of its own — the
        // very collision that made the old KnownNetworks property obsolete.
        var network = new System.Net.IPNetwork(IPAddress.Parse("10.0.0.0"), 8);

        var options = GetOptions(services => services.AddForwardingConfiguration(o => o.KnownIPNetworks.Add(network)));

        Assert.Equal([network], options.KnownIPNetworks);
    }

    [Fact]
    public void AddForwardingConfiguration_WithCallback_CanOverrideTheHeaders()
    {
        var options = GetOptions(services => services.AddForwardingConfiguration(
            o => o.ForwardedHeaders = ForwardedHeaders.XForwardedFor));

        Assert.Equal(ForwardedHeaders.XForwardedFor, options.ForwardedHeaders);
    }

    [Fact]
    public void AddForwardingConfiguration_WithNullServices_Throws()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(services.AddForwardingConfiguration);
    }

    [Fact]
    public void AddForwardingConfiguration_WithNullCallback_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddForwardingConfiguration(null!));
    }
}
