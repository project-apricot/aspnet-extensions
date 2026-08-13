using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.AspNetCore.Extensions.Cookies;

/// <summary>
/// Extensions for configuring the application's cookie policy.
/// </summary>
public static class CookieExtensions
{
    /// <summary>
    /// Configures the cookie policy to require HTTPS for every cookie, while leaving each
    /// cookie's own <c>SameSite</c> setting untouched.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <remarks>
    /// This configures the policy only. It takes effect once the application calls
    /// <c>UseCookiePolicy()</c>, which this method deliberately does not add — middleware order
    /// belongs to the application. Calling <c>Configure&lt;CookiePolicyOptions&gt;</c> afterwards
    /// composes with this, and the later value wins.
    /// </remarks>
    public static IServiceCollection AddSecureCookiePolicy(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.Configure<CookiePolicyOptions>(options =>
        {
            // Unspecified is the floor, not an override: it leaves a cookie that asked for
            // SameSite=None or Strict exactly as it was set. Raising the floor here would
            // silently rewrite cookies the application had already made a decision about.
            options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
            options.Secure = CookieSecurePolicy.Always;
        });
    }
}
