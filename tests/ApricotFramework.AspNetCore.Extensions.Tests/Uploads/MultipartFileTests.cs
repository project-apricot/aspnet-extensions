using System.Text;
using ApricotFramework.AspNetCore.Extensions.Uploads;

namespace ApricotFramework.AspNetCore.Extensions.Tests.Uploads;

public class MultipartFileTests
{
    /// <summary>
    /// A stream that refuses to report a length, as a request body does.
    /// </summary>
    private sealed class UnseekableStream : Stream
    {
        /// <summary>
        /// The bytes to serve.
        /// </summary>
        private readonly MemoryStream inner;

        /// <summary>
        /// Creates the stream.
        /// </summary>
        /// <param name="content">The content to serve.</param>
        public UnseekableStream(string content)
        {
            this.inner = new MemoryStream(Encoding.UTF8.GetBytes(content));
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
        public override int Read(byte[] buffer, int offset, int count) => this.inner.Read(buffer, offset, count);

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

    [Fact]
    public void Length_WithUnseekableStream_ReportsUnknown()
    {
        // The old implementation forwarded straight to Stream.Length, so reading IFormFile.Length on
        // a real upload threw NotSupportedException.
        using var stream = new UnseekableStream("hello");

        var file = new MultipartFile(stream, "field", "name.txt");

        Assert.Equal(MultipartFile.UnknownLength, file.Length);
    }

    [Fact]
    public void Length_WithSeekableStream_ReportsTheRealLength()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello"));

        var file = new MultipartFile(stream, "field", "name.txt");

        Assert.Equal(5, file.Length);
    }

    [Fact]
    public void UnknownLength_Always_IsNegative()
    {
        // Callers distinguish it by sign, so a non-negative sentinel would be indistinguishable from
        // a real length.
        Assert.True(MultipartFile.UnknownLength < 0);
    }

    [Fact]
    public void Constructor_WithNoContentType_ReportsEmptyStrings()
    {
        // IFormFile declares both as non-null, so an absent header has to read as empty rather than
        // handing the caller a null through a non-nullable property.
        using var stream = new MemoryStream();

        var file = new MultipartFile(stream, "field", "name.txt");

        Assert.Equal(string.Empty, file.ContentType);
        Assert.Equal(string.Empty, file.ContentDisposition);
    }

    [Fact]
    public void Constructor_WithContentTypeAndDisposition_ExposesBoth()
    {
        using var stream = new MemoryStream();

        var file = new MultipartFile(
            stream,
            "field",
            "name.txt",
            "image/png",
            "form-data; name=\"field\"; filename=\"name.txt\"");

        Assert.Equal("image/png", file.ContentType);
        Assert.Equal("form-data; name=\"field\"; filename=\"name.txt\"", file.ContentDisposition);
        Assert.Equal("image/png", file.Headers.ContentType);
    }

    [Fact]
    public void Constructor_Always_ExposesNameAndFileName()
    {
        using var stream = new MemoryStream();

        var file = new MultipartFile(stream, "field", "name.txt");

        Assert.Equal("field", file.Name);
        Assert.Equal("name.txt", file.FileName);
    }

    [Fact]
    public void OpenReadStream_Always_ReturnsTheSameStream()
    {
        using var stream = new MemoryStream();

        var file = new MultipartFile(stream, "field", "name.txt");

        Assert.Same(stream, file.OpenReadStream());
    }

    [Fact]
    public void CopyTo_Always_WritesEveryByte()
    {
        using var stream = new UnseekableStream("hello world");
        using var target = new MemoryStream();

        new MultipartFile(stream, "field", "name.txt").CopyTo(target);

        Assert.Equal("hello world", Encoding.UTF8.GetString(target.ToArray()));
    }

    [Fact]
    public async Task CopyToAsync_Always_WritesEveryByte()
    {
        using var stream = new UnseekableStream("hello world");
        using var target = new MemoryStream();

        await new MultipartFile(stream, "field", "name.txt")
            .CopyToAsync(target, TestContext.Current.CancellationToken);

        Assert.Equal("hello world", Encoding.UTF8.GetString(target.ToArray()));
    }

    [Fact]
    public void CopyTo_WithNullTarget_Throws()
    {
        using var stream = new MemoryStream();

        var file = new MultipartFile(stream, "field", "name.txt");

        Assert.Throws<ArgumentNullException>(() => file.CopyTo(null!));
    }

    [Fact]
    public async Task CopyToAsync_WithNullTarget_Throws()
    {
        using var stream = new MemoryStream();

        var file = new MultipartFile(stream, "field", "name.txt");

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => file.CopyToAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Constructor_WithNullStream_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MultipartFile(null!, "field", "name.txt"));
    }

    [Fact]
    public void Constructor_WithNullName_Throws()
    {
        using var stream = new MemoryStream();

        Assert.Throws<ArgumentNullException>(() => new MultipartFile(stream, null!, "name.txt"));
    }

    [Fact]
    public void Constructor_WithNullFileName_Throws()
    {
        using var stream = new MemoryStream();

        Assert.Throws<ArgumentNullException>(() => new MultipartFile(stream, "field", null!));
    }
}
