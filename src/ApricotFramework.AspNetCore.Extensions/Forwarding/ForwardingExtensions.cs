using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.AspNetCore.Extensions.Forwarding;

/// <summary>
/// Extensions for accepting the forwarded headers a reverse proxy adds.
/// </summary>
public static class ForwardingExtensions
{
    /// <summary>
    /// Configures <c>X-Forwarded-For</c>, <c>X-Forwarded-Proto</c> and <c>X-Forwarded-Host</c> to
    /// be honoured from any peer.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <remarks>
    /// Trusting every peer is only safe when the application cannot be reached except through the
    /// proxy, because any client that can connect directly is then able to choose its own
    /// apparent address, scheme and host. Use the overload taking a callback to name the real
    /// proxies where the deployment allows it.
    /// </remarks>
    public static IServiceCollection AddForwardingConfiguration(this IServiceCollection services)
    {
        return services.AddForwardingConfiguration(static _ => { });
    }

    /// <summary>
    /// Configures <c>X-Forwarded-For</c>, <c>X-Forwarded-Proto</c> and <c>X-Forwarded-Host</c>,
    /// then hands the options to a callback for further changes.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">
    /// Runs after the defaults above have been applied and the known-proxy lists cleared, so it
    /// can narrow the trust back down by adding entries to
    /// <see cref="ForwardedHeadersOptions.KnownProxies"/> or
    /// <see cref="ForwardedHeadersOptions.KnownIPNetworks"/>.
    /// </param>
    /// <returns>The service collection, for chaining.</returns>
    /// <remarks>
    /// Configuration alone changes nothing: the application must also call
    /// <c>UseForwardedHeaders()</c>, and it must come before anything that reads the client
    /// address or the scheme.
    /// </remarks>
    public static IServiceCollection AddForwardingConfiguration(this IServiceCollection services, Action<ForwardedHeadersOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        return services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                | ForwardedHeaders.XForwardedProto
                | ForwardedHeaders.XForwardedHost;

            // The defaults trust loopback only, which drops every header when the proxy is a
            // separate container or load balancer at an address the application cannot know in
            // advance. Clearing both lists accepts them from any peer.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            configure(options);
        });
    }
}
