namespace ApricotFramework.AspNetCore.Extensions.Examples.Web;

/// <summary>
/// The settings bound from the "Storage" configuration section by <c>BindAs</c>.
/// </summary>
public sealed class StorageSettings
{
    /// <summary>
    /// A value with a default, so an unbound section is still usable.
    /// </summary>
    public string Bucket { get; set; } = "default-bucket";

    /// <summary>
    /// A number, to show the binder converting types.
    /// </summary>
    public int MaxSizeMb { get; set; }

    /// <summary>
    /// A repeated key, to show the binder filling collections.
    /// </summary>
    public IList<string> Regions { get; } = [];
}

/// <summary>
/// A per-request item, used to demonstrate <c>GetItemOrDefault</c>.
/// </summary>
/// <param name="Name">Something to read back.</param>
public sealed record Tenant(string Name);
