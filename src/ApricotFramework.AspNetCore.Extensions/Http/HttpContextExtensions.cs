using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace ApricotFramework.AspNetCore.Extensions.Http;

/// <summary>
/// Extensions for reading from an <see cref="HttpContext"/> and its request.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Flattens a query string into a dictionary, collapsing single-valued keys to the value
    /// itself.
    /// </summary>
    /// <param name="query">The query collection to read.</param>
    /// <returns>
    /// A dictionary whose values are a <see cref="string"/> for a key given once, and a
    /// <see cref="List{T}"/> of <see cref="string"/> for a key repeated. A key present with no
    /// value maps to <see cref="string.Empty"/>.
    /// </returns>
    /// <remarks>
    /// Shaped for handing an arbitrary query on to something that serializes loosely typed values,
    /// such as an OAuth callback forwarded as JSON. Code that knows the keys it wants should read
    /// them from <see cref="IQueryCollection"/> directly rather than through this.
    /// <para>
    /// Keys are compared case insensitively, as they are in the query collection itself. A query
    /// string that spells one key two ways therefore arrives here already merged into a single
    /// entry with both values, and a lookup here answers whatever a lookup on the request would
    /// have.
    /// </para>
    /// </remarks>
    public static IDictionary<string, object> ToValuesDictionary(this IQueryCollection query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var values = new Dictionary<string, object>(query.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in query)
        {
            values[key] = value.Count switch
            {
                0 => string.Empty,
                1 => value[0] ?? string.Empty,
                _ => ToList(value),
            };
        }

        return values;
    }

    /// <summary>
    /// Reads a per-request item, falling back to a default.
    /// </summary>
    /// <param name="httpContext">The context whose items to read.</param>
    /// <param name="key">The key of the item.</param>
    /// <param name="defaultValue">Returned when the item is absent, null, or another type.</param>
    /// <typeparam name="TValue">The expected type of the item.</typeparam>
    /// <returns>The stored item, or <paramref name="defaultValue"/>.</returns>
    /// <remarks>
    /// An item stored under the same key by unrelated middleware yields the default rather than
    /// throwing, so one component's choice of key cannot crash another's read. Passing a non-null
    /// default makes the result non-null too, so callers who supply a fallback need no null check.
    /// </remarks>
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static TValue? GetItemOrDefault<TValue>(this HttpContext httpContext, string key, TValue? defaultValue)
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(key);

        return httpContext.Items.TryGetValue(key, out var item) && item is TValue value
            ? value
            : defaultValue;
    }

    /// <summary>
    /// Copies multiple header or query values into a list, substituting empty strings for nulls.
    /// </summary>
    /// <param name="source">The values to copy.</param>
    /// <returns>A list of the same length as <paramref name="source"/>.</returns>
    private static List<string> ToList(StringValues source)
    {
        var values = new List<string>(source.Count);

        foreach (var value in source)
        {
            values.Add(value ?? string.Empty);
        }

        return values;
    }
}
