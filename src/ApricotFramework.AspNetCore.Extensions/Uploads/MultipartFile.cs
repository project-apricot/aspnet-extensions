using Microsoft.AspNetCore.Http;

namespace ApricotFramework.AspNetCore.Extensions.Uploads;

/// <summary>
/// An <see cref="IFormFile"/> over one section of a multipart request, read forward only.
/// </summary>
/// <remarks>
/// The stream belongs to the reader that produced the section, so this type does not own or dispose
/// it, and it stays readable only until the next section is read.
/// </remarks>
public sealed class MultipartFile : IFormFile
{
    /// <summary>
    /// Matches the buffer the framework uses for its own stream copies.
    /// </summary>
    private const int CopyBufferSize = 80 * 1024;

    /// <summary>
    /// Reported by <see cref="Length"/> when the size is not knowable.
    /// </summary>
    public const long UnknownLength = -1;

    /// <summary>
    /// The section body.
    /// </summary>
    private readonly Stream stream;

    /// <summary>
    /// Creates a form file over a section's stream.
    /// </summary>
    /// <param name="stream">The section body, read forward only.</param>
    /// <param name="name">The form field name the file was posted under.</param>
    /// <param name="fileName">The file name the client supplied.</param>
    /// <param name="contentType">The section's content type, if it declared one.</param>
    /// <param name="contentDisposition">The section's raw content disposition, if it declared one.</param>
    public MultipartFile(Stream stream, string name, string fileName, string? contentType = null, string? contentDisposition = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(fileName);

        this.stream = stream;
        this.Name = name;
        this.FileName = fileName;
        this.Headers = new HeaderDictionary();

        if (contentType is not null)
        {
            this.Headers.ContentType = contentType;
        }

        if (contentDisposition is not null)
        {
            this.Headers.ContentDisposition = contentDisposition;
        }
    }

    /// <inheritdoc />
    public IHeaderDictionary Headers { get; }

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="UnknownLength"/> while streaming. A multipart section is read forward only over
    /// the request body, so its size is not known until the section has been consumed — and asking
    /// the stream for it would throw. Callers that need a byte count should count what they copy.
    /// </remarks>
    public long Length => this.stream.CanSeek ? this.stream.Length : UnknownLength;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string FileName { get; }

    /// <inheritdoc />
    public string ContentType => this.Headers.ContentType.ToString();

    /// <inheritdoc />
    public string ContentDisposition => this.Headers.ContentDisposition.ToString();

    /// <inheritdoc />
    public Stream OpenReadStream()
    {
        return this.stream;
    }

    /// <inheritdoc />
    public void CopyTo(Stream target)
    {
        ArgumentNullException.ThrowIfNull(target);

        this.stream.CopyTo(target, CopyBufferSize);
    }

    /// <inheritdoc />
    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);

        return this.stream.CopyToAsync(target, CopyBufferSize, cancellationToken);
    }
}
