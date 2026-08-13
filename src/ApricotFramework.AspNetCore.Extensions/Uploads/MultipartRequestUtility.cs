using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace ApricotFramework.AspNetCore.Extensions.Uploads;

/// <summary>
/// Helpers for inspecting a multipart request and its sections.
/// </summary>
public static class MultipartRequestUtility
{
    /// <summary>
    /// The media type prefix every multipart content type starts with.
    /// </summary>
    private const string MultipartPrefix = "multipart/";

    /// <summary>
    /// Reads the boundary that separates the sections of a multipart body.
    /// </summary>
    /// <param name="contentType">The parsed request content type.</param>
    /// <param name="lengthLimit">
    /// The longest boundary to accept. RFC 2046 §5.1 puts the ceiling at 70 characters, and
    /// <c>FormOptions.MultipartBoundaryLengthLimit</c> is the framework's own setting for it.
    /// </param>
    /// <returns>The boundary, with any surrounding quotes removed.</returns>
    /// <exception cref="InvalidDataException">
    /// The content type declares no boundary, or one longer than <paramref name="lengthLimit"/>.
    /// </exception>
    public static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit)
    {
        ArgumentNullException.ThrowIfNull(contentType);

        var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

        if (string.IsNullOrWhiteSpace(boundary))
        {
            throw new InvalidDataException("Missing content-type boundary.");
        }

        if (boundary.Length > lengthLimit)
        {
            throw new InvalidDataException($"Multipart boundary length limit {lengthLimit} exceeded.");
        }

        return boundary;
    }

    /// <summary>
    /// Whether a content type describes a multipart body.
    /// </summary>
    /// <param name="contentType">The content type to test, which may be absent.</param>
    /// <returns><see langword="true"/> for any <c>multipart/*</c> media type.</returns>
    /// <remarks>
    /// Anchored at the start of the value, so a content type that merely mentions
    /// <c>multipart/</c> in a parameter is not mistaken for one.
    /// </remarks>
    public static bool IsMultipartContentType([NotNullWhen(true)] string? contentType)
    {
        return contentType is not null
            && contentType.StartsWith(MultipartPrefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Reads the character encoding of a multipart section.
    /// </summary>
    /// <param name="section">The section to inspect.</param>
    /// <returns>The declared encoding, or UTF-8 when the section does not name one.</returns>
    /// <remarks>
    /// A section that omits <c>charset</c> still parses, leaving the encoding unset, so the absent
    /// case has to be handled separately from an unparseable header.
    /// </remarks>
    public static Encoding GetEncoding(MultipartSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return MediaTypeHeaderValue.TryParse(section.ContentType, out var mediaType) && mediaType.Encoding is not null
            ? mediaType.Encoding
            : Encoding.UTF8;
    }
}
