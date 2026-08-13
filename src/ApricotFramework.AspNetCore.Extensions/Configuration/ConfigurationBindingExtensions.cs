using Microsoft.Extensions.Configuration;

namespace ApricotFramework.AspNetCore.Extensions.Configuration;

/// <summary>
/// Extensions for reading strongly typed settings out of an <see cref="IConfiguration"/>.
/// </summary>
public static class ConfigurationBindingExtensions
{
    /// <summary>
    /// Binds a configuration section to a new instance of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="configuration">The configuration to read from.</param>
    /// <param name="name">The name of the section to bind.</param>
    /// <typeparam name="T">The settings type to create and populate.</typeparam>
    /// <returns>
    /// A new instance with the section's values applied. A missing or empty section yields an
    /// instance carrying only its own defaults, never <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// For settings that should track configuration reloads, prefer
    /// <c>services.AddOptions&lt;T&gt;().Bind(...)</c>. This returns a detached snapshot taken at
    /// the moment of the call.
    /// </remarks>
    public static T BindAs<T>(this IConfiguration configuration, string name)
        where T : new()
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var settings = new T();

        configuration.GetSection(name).Bind(settings);

        return settings;
    }
}
