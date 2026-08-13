using ApricotFramework.AspNetCore.Extensions.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApricotFramework.AspNetCore.Extensions.Tests.Cookies;

public class CookieExtensionsTests
{
    /// <summary>
    /// A user agent from one of the browsers the removed sniffing used to single out.
    /// </summary>
    private const string LegacyIosUserAgent =
        "Mozilla/5.0 (iPhone; CPU iPhone OS 12_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/12.0 Mobile/15E148 Safari/604.1";

    /// <summary>
    /// A modern Chrome user agent whose major version begins with 5, which the removed sniffing
    /// matched on as though it were Chrome 5x.
    /// </summary>
    private const string FutureChromeUserAgent =
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/512.0.0.0 Safari/537.36";

    /// <summary>
    /// Builds a provider with the policy registered.
    /// </summary>
    /// <returns>The provider, which the caller disposes.</returns>
    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSecureCookiePolicy();

        return services.BuildServiceProvider(validateScopes: true);
    }

    /// <summary>
    /// Runs the cookie policy middleware over a request that appends one cookie.
    /// </summary>
    /// <param name="userAgent">The user agent to send, or null for none.</param>
    /// <param name="cookieOptions">The options the application sets on its cookie.</param>
    /// <returns>The resulting <c>Set-Cookie</c> header.</returns>
    private static async Task<string> AppendCookieAsync(string? userAgent, CookieOptions cookieOptions)
    {
        using var provider = CreateProvider();

        var context = new DefaultHttpContext();

        context.Request.Scheme = "https";

        if (userAgent is not null)
        {
            context.Request.Headers.UserAgent = userAgent;
        }

        var middleware = new CookiePolicyMiddleware(
            _ =>
            {
                context.Response.Cookies.Append("session", "value", cookieOptions);

                return Task.CompletedTask;
            },
            provider.GetRequiredService<IOptions<CookiePolicyOptions>>(),
            provider.GetRequiredService<ILoggerFactory>());

        await middleware.Invoke(context);

        return context.Response.Headers.SetCookie.ToString();
    }

    [Fact]
    public void AddSecureCookiePolicy_Always_RequiresSecureCookies()
    {
        using var provider = CreateProvider();

        var options = provider.GetRequiredService<IOptions<CookiePolicyOptions>>().Value;

        Assert.Equal(CookieSecurePolicy.Always, options.Secure);
    }

    [Fact]
    public void AddSecureCookiePolicy_Always_LeavesTheSameSiteFloorUnspecified()
    {
        using var provider = CreateProvider();

        var options = provider.GetRequiredService<IOptions<CookiePolicyOptions>>().Value;

        Assert.Equal(SameSiteMode.Unspecified, options.MinimumSameSitePolicy);
    }

    [Fact]
    public void AddSecureCookiePolicy_Always_InstallsNoUserAgentHooks()
    {
        // The sniffing this library used to do was installed as OnAppendCookie and OnDeleteCookie
        // handlers. Their absence is what makes the removal verifiable rather than assumed.
        using var provider = CreateProvider();

        var options = provider.GetRequiredService<IOptions<CookiePolicyOptions>>().Value;

        Assert.Null(options.OnAppendCookie);
        Assert.Null(options.OnDeleteCookie);
    }

    [Fact]
    public async Task AddSecureCookiePolicy_WithSameSiteNone_KeepsSameSiteNone()
    {
        var header = await AppendCookieAsync(
            userAgent: null,
            new CookieOptions { SameSite = SameSiteMode.None, Secure = true });

        Assert.Contains("samesite=none", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddSecureCookiePolicy_WithLegacyIosUserAgent_StillKeepsSameSiteNone()
    {
        // The removed workaround downgraded this to Unspecified. Those browsers are long gone, and
        // downgrading now would weaken a cookie the application deliberately marked None.
        var header = await AppendCookieAsync(
            LegacyIosUserAgent,
            new CookieOptions { SameSite = SameSiteMode.None, Secure = true });

        Assert.Contains("samesite=none", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddSecureCookiePolicy_WithChromeMajorVersionStartingInFive_StillKeepsSameSiteNone()
    {
        // This is the case that made the removal urgent rather than merely tidy: the old substring
        // test for "Chrome/5" matches Chrome 512 as readily as Chrome 51.
        var header = await AppendCookieAsync(
            FutureChromeUserAgent,
            new CookieOptions { SameSite = SameSiteMode.None, Secure = true });

        Assert.Contains("samesite=none", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddSecureCookiePolicy_WithInsecureCookie_AddsSecure()
    {
        var header = await AppendCookieAsync(
            userAgent: null,
            new CookieOptions { SameSite = SameSiteMode.Lax, Secure = false });

        Assert.Contains("secure", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddSecureCookiePolicy_WithSameSiteStrict_LeavesStrictAlone()
    {
        var header = await AppendCookieAsync(
            userAgent: null,
            new CookieOptions { SameSite = SameSiteMode.Strict });

        Assert.Contains("samesite=strict", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddSecureCookiePolicy_WithLaterConfigure_LetsTheLaterValueWin()
    {
        var services = new ServiceCollection();

        services.AddSecureCookiePolicy();
        services.Configure<CookiePolicyOptions>(options => options.Secure = CookieSecurePolicy.SameAsRequest);

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Equal(
            CookieSecurePolicy.SameAsRequest,
            provider.GetRequiredService<IOptions<CookiePolicyOptions>>().Value.Secure);
    }

    [Fact]
    public void AddSecureCookiePolicy_WithNullServices_Throws()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(services.AddSecureCookiePolicy);
    }
}
