using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace ApricotFramework.AspNetCore.Extensions.Uploads;

/// <summary>
/// Extensions for reading a multipart request without buffering it.
/// </summary>
public static class HttpRequestUploadExtensions
{
    /// <summary>
    /// The framework's own limits, used as the ceilings for boundary length, field length and
    /// field count.
    /// </summary>
    private static readonly FormOptions DefaultFormOptions = new();

    /// <summary>
    /// The character buffer used while reading a non-file field.
    /// </summary>
    private const int FieldBufferSize = 1024;

    /// <summary>
    /// Reads a multipart request section by section, handing each file to a callback as it arrives
    /// and collecting the non-file fields.
    /// </summary>
    /// <param name="request">The multipart request to read.</param>
    /// <param name="onFile">
    /// Invoked once per file section. The file's stream is only readable for the duration of the
    /// call, because the next section cannot be reached until this one has been consumed.
    /// </param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The non-file form fields, in the order the request declared them.</returns>
    /// <exception cref="InvalidOperationException">
    /// The request is not multipart, or its body has already been consumed — see the remarks.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// The body is malformed, or exceeds the boundary, field length or field count limits.
    /// </exception>
    /// <exception cref="IOException">The body ended early, or the connection was lost.</exception>
    /// <exception cref="BadHttpRequestException">
    /// The server refused the request, most often because the body exceeded its size limit. Passed
    /// through untouched so its status code survives.
    /// </exception>
    /// <remarks>
    /// Nothing is written to disk or held in memory, so a request larger than available memory is
    /// fine as long as <paramref name="onFile"/> streams rather than buffers. The action must carry
    /// <see cref="ModelBinding.DisableFormValueModelBindingAttribute"/>, or MVC will have read the
    /// body first and there will be nothing left to stream.
    /// <para>
    /// No limit is placed on a file's size here. The one that bites is the server's: Kestrel caps a
    /// request body at 30 MB by default and answers <c>413</c> above it. Raise it with
    /// <c>ConfigureKestrel(o =&gt; o.Limits.MaxRequestBodySize = …)</c> — in code, because
    /// <c>Limits</c> is not bound from the <c>Kestrel</c> configuration section. The 128 MB
    /// <c>FormOptions.MultipartBodyLengthLimit</c> does not apply, belonging to
    /// <c>ReadFormAsync</c>, which this bypasses.
    /// </para>
    /// <para>
    /// Non-file fields are capped at <c>FormOptions.ValueLengthLimit</c>, which the framework's own
    /// multipart reader does not enforce for them. A single 8 MB field that <c>ReadFormAsync</c>
    /// accepts is rejected here, deliberately: the size of a field is the client's choice, and
    /// reading one to its end unconditionally lets a request decide how much memory to allocate.
    /// </para>
    /// <para>
    /// Every exception above describes a bad request rather than a server fault, but ASP.NET Core
    /// turns none of them into a <c>400</c> on its own — no more than it does for its own
    /// <c>ReadFormAsync</c>. Map them if a malformed upload should not be reported as a 500.
    /// </para>
    /// </remarks>
    public static async Task<IFormCollection> StreamFilesAsync(
        this HttpRequest request,
        Func<IFormFile, Task> onFile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(onFile);

        var contentType = request.ContentType;

        if (!MultipartRequestUtility.IsMultipartContentType(contentType))
        {
            throw new InvalidOperationException($"Expected a multipart request, but the content type was '{contentType}'.");
        }

        var boundary = MultipartRequestUtility.GetBoundary(
            MediaTypeHeaderValue.Parse(contentType),
            DefaultFormOptions.MultipartBoundaryLengthLimit);

        var reader = new MultipartReader(boundary, request.Body);
        var fields = new KeyValueAccumulator();

        var section = await ReadFirstSectionAsync(reader, cancellationToken);

        while (section is not null)
        {
            if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
            {
                if (contentDisposition.IsFileDisposition())
                {
                    await InvokeForFileAsync(section, onFile);
                }
                else if (contentDisposition.IsFormDisposition())
                {
                    var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name).Value;

                    // A field with no name cannot be bound to anything, and an empty key in the
                    // form collection would only be a trap for the caller.
                    if (!string.IsNullOrEmpty(key))
                    {
                        fields.Append(key, await ReadFieldValueAsync(section, cancellationToken));

                        if (fields.ValueCount > DefaultFormOptions.ValueCountLimit)
                        {
                            throw new InvalidDataException($"Form value count limit {DefaultFormOptions.ValueCountLimit} exceeded.");
                        }
                    }
                }
            }

            // Reading the next section drains whatever is left of this one, so a callback that
            // ignored its file does not desynchronise the reader.
            section = await reader.ReadNextSectionAsync(cancellationToken);
        }

        return fields.HasValues ? new FormCollection(fields.GetResults()) : FormCollection.Empty;
    }

    /// <summary>
    /// Reads the first section, translating an unreadable body into an actionable error.
    /// </summary>
    /// <param name="reader">The reader over the request body.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The first section, or null for an empty body.</returns>
    /// <exception cref="InvalidOperationException">The body could not be read at all.</exception>
    /// <remarks>
    /// Failing on the very first read almost always means model binding already consumed the body,
    /// which is a wiring mistake rather than a bad request. Saying so beats reporting an empty
    /// upload, which is what silently treating it as end-of-stream used to do.
    /// <para>
    /// <see cref="BadHttpRequestException"/> is deliberately let through. It derives from
    /// <see cref="IOException"/>, so a plain catch would swallow it — turning the server's own
    /// account of what was wrong with the request, and the status code it carries, into a guess
    /// about model binding. A body over the configured size limit arrives this way.
    /// </para>
    /// </remarks>
    private static async Task<MultipartSection?> ReadFirstSectionAsync(MultipartReader reader, CancellationToken cancellationToken)
    {
        try
        {
            return await reader.ReadNextSectionAsync(cancellationToken);
        }
        catch (IOException exception) when (exception is not BadHttpRequestException)
        {
            throw new InvalidOperationException(
                $"The request body could not be read as multipart; it may already have been consumed. Apply [{nameof(ModelBinding.DisableFormValueModelBindingAttribute)}] to the action so model binding leaves the body alone.",
                exception);
        }
    }

    /// <summary>
    /// Wraps a file section as an <see cref="IFormFile"/> and hands it to the callback.
    /// </summary>
    /// <param name="section">The file section.</param>
    /// <param name="onFile">The callback to invoke.</param>
    /// <exception cref="InvalidDataException">The section could not be read as a file.</exception>
    private static async Task InvokeForFileAsync(MultipartSection section, Func<IFormFile, Task> onFile)
    {
        var fileSection = section.AsFileSection();

        if (fileSection?.FileStream is null)
        {
            throw new InvalidDataException("A section declared a file disposition but carried no readable file.");
        }

        var file = new MultipartFile(
            fileSection.FileStream,
            fileSection.Name,
            fileSection.FileName,
            section.ContentType,
            section.ContentDisposition);

        await onFile(file);
    }

    /// <summary>
    /// Reads a non-file section's body as text, refusing one that is too long.
    /// </summary>
    /// <param name="section">The section to read.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The field's value.</returns>
    /// <exception cref="InvalidDataException">The value exceeded the length limit.</exception>
    /// <remarks>
    /// Bounded deliberately. The size of a field is chosen by the client, so reading one to its end
    /// unconditionally lets a single request decide how much memory to allocate.
    /// </remarks>
    private static async Task<string> ReadFieldValueAsync(MultipartSection section, CancellationToken cancellationToken)
    {
        var limit = DefaultFormOptions.ValueLengthLimit;

        using var reader = new StreamReader(
            section.Body,
            MultipartRequestUtility.GetEncoding(section),
            detectEncodingFromByteOrderMarks: true,
            bufferSize: FieldBufferSize,
            leaveOpen: true);

        var buffer = new char[FieldBufferSize];
        var value = new StringBuilder();

        while (true)
        {
            var read = await reader.ReadAsync(buffer, cancellationToken);

            if (read == 0)
            {
                break;
            }

            if (value.Length + read > limit)
            {
                throw new InvalidDataException($"Form value length limit {limit} exceeded.");
            }

            value.Append(buffer, 0, read);
        }

        return value.ToString();
    }
}
