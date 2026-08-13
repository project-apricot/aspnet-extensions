using ApricotFramework.AspNetCore.Extensions.Cors;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApricotFramework.AspNetCore.Extensions.Tests.Cors;

public class CorsExtensionsTests
{
    /// <summary>
    /// Resolves a registered policy by name.
    /// </summary>
    /// <param name="configure">Registers the policy under test.</param>
    /// <param name="name">The policy to read back.</param>
    /// <returns>The built policy.</returns>
    private static CorsPolicy GetPolicy(Action<IServiceCollection> configure, string name)
    {
        var services = new ServiceCollection();

        configure(services);

        using var provider = services.BuildServiceProvider(validateScopes: true);

        var policy = provider.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy(name);

        Assert.NotNull(policy);

        return policy;
    }

    [Fact]
    public void DefaultPolicyName_Always_KeepsItsPublishedValue()
    {
        // An application may name this policy in configuration or an [EnableCors] attribute, so the
        // string is a contract rather than an implementation detail.
        Assert.Equal("DefaultCorsPolicy", CorsExtensions.DefaultPolicyName);
    }

    [Fact]
    public void AddAnyOriginCors_WithNoName_RegistersUnderTheDefaultName()
    {
        var policy = GetPolicy(
            services => services.AddAnyOriginCors(),
            CorsExtensions.DefaultPolicyName);

        Assert.True(policy.AllowAnyOrigin);
    }

    [Fact]
    public void AddAnyOriginCors_WithName_RegistersUnderThatName()
    {
        var policy = GetPolicy(services => services.AddAnyOriginCors("Custom"), "Custom");

        Assert.True(policy.AllowAnyOrigin);
    }

    [Fact]
    public void AddAnyOriginCors_Always_AllowsAnyMethodAndHeader()
    {
        var policy = GetPolicy(services => services.AddAnyOriginCors(), CorsExtensions.DefaultPolicyName);

        Assert.True(policy.AllowAnyMethod);
        Assert.True(policy.AllowAnyHeader);
    }

    [Fact]
    public void AddAnyOriginCors_Always_DoesNotSupportCredentials()
    {
        var policy = GetPolicy(services => services.AddAnyOriginCors(), CorsExtensions.DefaultPolicyName);

        Assert.False(policy.SupportsCredentials);
    }

    [Fact]
    public void AddAnyOriginCors_WithName_LeavesOtherNamesUnregistered()
    {
        var services = new ServiceCollection();

        services.AddAnyOriginCors("Custom");

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Null(provider.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy(CorsExtensions.DefaultPolicyName));
    }

    [Fact]
    public void CorsPolicyBuilder_WithAnyOriginAndCredentials_Throws()
    {
        // Pins the framework behaviour the documentation on AddAnyOriginCors describes, so the doc
        // cannot drift from what actually happens.
        var builder = new CorsPolicyBuilder();

        builder.AllowAnyOrigin().AllowCredentials();

        Assert.Throws<InvalidOperationException>(builder.Build);
    }

    [Fact]
    public void AddAnyOriginCors_WithNullServices_Throws()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() => services.AddAnyOriginCors("Custom"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddAnyOriginCors_WithoutName_Throws(string? name)
    {
        var services = new ServiceCollection();

        Assert.ThrowsAny<ArgumentException>(() => services.AddAnyOriginCors(name!));
    }
}
