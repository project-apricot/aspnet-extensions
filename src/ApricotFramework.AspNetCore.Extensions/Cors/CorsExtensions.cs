using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.AspNetCore.Extensions.Cors;

/// <summary>
/// Extensions for registering a fully permissive CORS policy.
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// The name of the policy registered when no other name is given.
    /// </summary>
    /// <remarks>
    /// The value is part of the library's contract, not an implementation detail: an application
    /// may name this policy in configuration or in an <c>[EnableCors]</c> attribute.
    /// </remarks>
    public const string DefaultPolicyName = "DefaultCorsPolicy";

    /// <summary>
    /// Adds a CORS policy named <see cref="DefaultPolicyName"/> that allows any origin, method
    /// and header.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddAnyOriginCors(this IServiceCollection services)
    {
        return services.AddAnyOriginCors(DefaultPolicyName);
    }

    /// <summary>
    /// Adds a named CORS policy that allows any origin, method and header.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="name">The policy name, as passed to <c>UseCors</c> or <c>[EnableCors]</c>.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <remarks>
    /// The policy cannot carry credentials. CORS forbids answering a credentialed request with
    /// <c>Access-Control-Allow-Origin: *</c>, and ASP.NET Core enforces that rather than leaving it
    /// to the browser: adding <c>AllowCredentials</c> to a wildcard-origin policy throws
    /// <see cref="InvalidOperationException"/> when the policy is built. Name the permitted origins
    /// explicitly when cookies or an <c>Authorization</c> header have to survive the request.
    /// </remarks>
    public static IServiceCollection AddAnyOriginCors(this IServiceCollection services, string name)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return services.AddCors(options => options.AddPolicy(name, builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()));
    }
}
