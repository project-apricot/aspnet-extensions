using Microsoft.AspNetCore.Http;

namespace ApricotFramework.AspNetCore.Extensions.Tests.Uploads;

/// <summary>
/// Builds requests whose bodies behave like a real one: forward only, unknown length.
/// </summary>
internal static class MultipartRequests
{
    /// <summary>
    /// Wraps a captured fixture as an incoming request.
    /// </summary>
    /// <param name="fixture">The captured body and content type.</param>
    /// <returns>A request positioned at the start of the body.</returns>
    public static HttpRequest From(MultipartFixtures.Fixture fixture)
    {
        return With(fixture.ContentType, fixture.GetBody());
    }

    /// <summary>
    /// Wraps arbitrary bytes as an incoming request.
    /// </summary>
    /// <param name="contentType">The content type to advertise.</param>
    /// <param name="body">The raw body bytes.</param>
    /// <returns>A request positioned at the start of the body.</returns>
    public static HttpRequest With(string? contentType, byte[] body)
    {
        var context = new DefaultHttpContext();

        context.Request.ContentType = contentType;
        context.Request.ContentLength = body.Length;
        context.Request.Body = new ForwardOnlyStream(body);

        return context.Request;
    }

    /// <summary>
    /// Wraps a body that fails on the first read, the way the server rejects one it will not accept.
    /// </summary>
    /// <param name="contentType">The content type to advertise.</param>
    /// <param name="failure">The exception the body should throw.</param>
    /// <returns>A request whose body cannot be read.</returns>
    public static HttpRequest Failing(string contentType, Exception failure)
    {
        var context = new DefaultHttpContext();

        context.Request.ContentType = contentType;
        context.Request.Body = new FailingStream(failure);

        return context.Request;
    }

    /// <summary>
    /// A body that throws instead of producing bytes.
    /// </summary>
    private sealed class FailingStream : Stream
    {
        /// <summary>
        /// The exception every read raises.
        /// </summary>
        private readonly Exception failure;

        /// <summary>
        /// Creates the stream.
        /// </summary>
        /// <param name="failure">The exception to raise.</param>
        public FailingStream(Exception failure)
        {
            this.failure = failure;
        }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) => throw this.failure;

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>
    /// A stream with the same restrictions Kestrel's request body has.
    /// </summary>
    /// <remarks>
    /// A <see cref="MemoryStream"/> would be seekable and would report a length, which is exactly
    /// what a real request body does not do. Testing against one would hide any code that depends
    /// on either — including <c>MultipartFile.Length</c>, whose whole reason for existing is that
    /// the length is unavailable here.
    /// </remarks>
    private sealed class ForwardOnlyStream : Stream
    {
        /// <summary>
        /// The bytes still to be handed out.
        /// </summary>
        private readonly MemoryStream inner;

        /// <summary>
        /// Creates the stream over a body.
        /// </summary>
        /// <param name="body">The bytes to serve.</param>
        public ForwardOnlyStream(byte[] body)
        {
            this.inner = new MemoryStream(body, writable: false);
        }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => this.inner.Position;
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.inner.Read(buffer, offset, count);
        }

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
