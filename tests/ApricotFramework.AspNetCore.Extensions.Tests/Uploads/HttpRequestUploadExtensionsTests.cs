using System.Text;
using ApricotFramework.AspNetCore.Extensions.Uploads;
using Microsoft.AspNetCore.Http;

namespace ApricotFramework.AspNetCore.Extensions.Tests.Uploads;

public class HttpRequestUploadExtensionsTests
{
    /// <summary>
    /// The token every read in these tests runs under.
    /// </summary>
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>
    /// Reads a file to the end, which is all a real consumer does with the stream.
    /// </summary>
    /// <param name="file">The file handed to the callback.</param>
    /// <returns>The file's content as text.</returns>
    private static async Task<string> ReadAsync(IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);

        return await reader.ReadToEndAsync(Token);
    }

    [Fact]
    public async Task StreamFilesAsync_WithOneFile_HandsTheContentToTheCallback()
    {
        var request = MultipartRequests.From(MultipartFixtures.OneFile);
        var seen = new List<string>();

        var form = await request.StreamFilesAsync(async file => seen.Add(await ReadAsync(file)), Token);

        Assert.Equal(["hello world"], seen);
        Assert.Empty(form);
    }

    [Fact]
    public async Task StreamFilesAsync_WithOneFile_ExposesNameAndFileNameAndContentType()
    {
        var request = MultipartRequests.From(MultipartFixtures.OneFile);
        var names = new List<(string Name, string FileName, string ContentType)>();

        await request.StreamFilesAsync(
            file =>
            {
                names.Add((file.Name, file.FileName, file.ContentType));

                return Task.CompletedTask;
            },
            Token);

        Assert.Equal([("file", "small.txt", "text/plain")], names);
    }

    [Fact]
    public async Task StreamFilesAsync_WithTwoFiles_VisitsBothInOrder()
    {
        var request = MultipartRequests.From(MultipartFixtures.TwoFiles);
        var seen = new List<string>();

        await request.StreamFilesAsync(async file => seen.Add($"{file.Name}={await ReadAsync(file)}"), Token);

        Assert.Equal(["file1=hello world", "file2=line one\nline two\n"], seen);
    }

    [Fact]
    public async Task StreamFilesAsync_WithFilesAndFields_ReturnsTheFieldsAndStreamsTheFiles()
    {
        var request = MultipartRequests.From(MultipartFixtures.Mixed);
        var seen = new List<string>();

        var form = await request.StreamFilesAsync(async file => seen.Add(await ReadAsync(file)), Token);

        Assert.Equal(["hello world"], seen);
        Assert.Equal("Report", form["title"]);
    }

    [Fact]
    public async Task StreamFilesAsync_WithRepeatedFieldKey_KeepsEveryValue()
    {
        var request = MultipartRequests.From(MultipartFixtures.Mixed);

        var form = await request.StreamFilesAsync(static _ => Task.CompletedTask, Token);
        var tags = form["tag"];

        Assert.Equal(2, tags.Count);
        Assert.Equal("a", tags[0]);
        Assert.Equal("b", tags[1]);
    }

    [Fact]
    public async Task StreamFilesAsync_WithNoFiles_ReturnsEveryFieldAndCallsBackNever()
    {
        var request = MultipartRequests.From(MultipartFixtures.FieldsOnly);
        var callbacks = 0;

        var form = await request.StreamFilesAsync(
            _ =>
            {
                callbacks++;

                return Task.CompletedTask;
            },
            Token);

        Assert.Equal(0, callbacks);
        Assert.Equal("one", form["alpha"]);
        Assert.Equal("two", form["beta"]);
    }

    [Fact]
    public async Task StreamFilesAsync_WithFieldDeclaringNoCharset_ReadsItAsUtf8()
    {
        // These captured sections carry no Content-Type at all, so the encoding can only come from
        // the UTF-8 fallback rather than from the header.
        var request = MultipartRequests.From(MultipartFixtures.FieldsOnly);

        var form = await request.StreamFilesAsync(static _ => Task.CompletedTask, Token);

        Assert.Equal("one", form["alpha"]);
    }

    [Fact]
    public async Task StreamFilesAsync_WithFieldDeclaringCharset_HonoursIt()
    {
        var request = MultipartRequests.From(MultipartFixtures.CharsetField);

        var form = await request.StreamFilesAsync(static _ => Task.CompletedTask, Token);

        Assert.Equal("café", form["note"]);
    }

    [Fact]
    public async Task StreamFilesAsync_WithNonAsciiFileName_PreservesNameAndContent()
    {
        var request = MultipartRequests.From(MultipartFixtures.UnicodeFile);
        var seen = new List<(string FileName, string Content)>();

        await request.StreamFilesAsync(async file => seen.Add((file.FileName, await ReadAsync(file))), Token);

        Assert.Equal([("unicode-café.txt", "café naïve")], seen);
    }

    [Fact]
    public async Task StreamFilesAsync_WhileStreaming_ReportsLengthAsUnknown()
    {
        var request = MultipartRequests.From(MultipartFixtures.OneFile);
        var lengths = new List<long>();

        await request.StreamFilesAsync(
            file =>
            {
                lengths.Add(file.Length);

                return Task.CompletedTask;
            },
            Token);

        Assert.Equal([MultipartFile.UnknownLength], lengths);
    }

    [Fact]
    public async Task StreamFilesAsync_WithNonMultipartContentType_Throws()
    {
        var request = MultipartRequests.With("application/json", Encoding.UTF8.GetBytes("{}"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => request.StreamFilesAsync(static _ => Task.CompletedTask, Token));

        Assert.Contains("application/json", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StreamFilesAsync_WithContentTypeMentioningMultipartLater_Throws()
    {
        // The old check searched anywhere in the value, so this passed as multipart.
        var request = MultipartRequests.With("text/plain; note=multipart/form-data", [1, 2, 3]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => request.StreamFilesAsync(static _ => Task.CompletedTask, Token));
    }

    [Fact]
    public async Task StreamFilesAsync_WithNoContentType_Throws()
    {
        var request = MultipartRequests.With(contentType: null, []);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => request.StreamFilesAsync(static _ => Task.CompletedTask, Token));
    }

    [Fact]
    public async Task StreamFilesAsync_WithNoBoundary_Throws()
    {
        var request = MultipartRequests.With("multipart/form-data", []);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => request.StreamFilesAsync(static _ => Task.CompletedTask, Token));
    }

    [Fact]
    public async Task StreamFilesAsync_WithOverlongBoundary_Throws()
    {
        var request = MultipartRequests.With($"multipart/form-data; boundary={new string('b', 200)}", []);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => request.StreamFilesAsync(static _ => Task.CompletedTask, Token));

        Assert.Contains("boundary length limit", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StreamFilesAsync_WithEmptyBody_ReportsTheBodyAsUnreadable()
    {
        // An empty body is indistinguishable from one another component already consumed, and that
        // is by far the likelier cause, so the message points at the wiring mistake.
        var request = MultipartRequests.With("multipart/form-data; boundary=abc", []);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => request.StreamFilesAsync(static _ => Task.CompletedTask, Token));

        Assert.Contains("DisableFormValueModelBindingAttribute", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StreamFilesAsync_WhenTheServerRejectsTheBody_LetsThatExceptionThrough()
    {
        // BadHttpRequestException derives from IOException, so the "body already consumed" catch has
        // to exclude it. Swallowing it replaces the server's own account of the problem — an
        // oversized body, most often — and discards the status code it carries.
        var request = MultipartRequests.Failing(
            "multipart/form-data; boundary=abc",
            new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge));

        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
            () => request.StreamFilesAsync(static _ => Task.CompletedTask, Token));

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, exception.StatusCode);
    }

    [Fact]
    public async Task StreamFilesAsync_WhenTheBodyIsPlainlyUnreadable_StillReportsTheWiringMistake()
    {
        var request = MultipartRequests.Failing(
            "multipart/form-data; boundary=abc",
            new IOException("Unexpected end of Stream."));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => request.StreamFilesAsync(static _ => Task.CompletedTask, Token));

        Assert.Contains("DisableFormValueModelBindingAttribute", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StreamFilesAsync_WithNullCallback_Throws()
    {
        var request = MultipartRequests.From(MultipartFixtures.OneFile);

        await Assert.ThrowsAsync<ArgumentNullException>(() => request.StreamFilesAsync(null!, Token));
    }

    [Fact]
    public async Task StreamFilesAsync_WhenCancelled_Throws()
    {
        var request = MultipartRequests.From(MultipartFixtures.TwoFiles);
        using var cancellation = new CancellationTokenSource();

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => request.StreamFilesAsync(static _ => Task.CompletedTask, cancellation.Token));
    }

    [Fact]
    public async Task StreamFilesAsync_WhenCallbackThrows_SurfacesTheCallbackException()
    {
        var request = MultipartRequests.From(MultipartFixtures.OneFile);

        await Assert.ThrowsAsync<InvalidTimeZoneException>(
            () => request.StreamFilesAsync(static _ => throw new InvalidTimeZoneException("from the callback"), Token));
    }

    [Fact]
    public async Task StreamFilesAsync_WhenCallbackIgnoresTheFile_StillReadsLaterSections()
    {
        // The file comes first in this fixture, so the fields after it are only reachable if the
        // unread file section is drained on the way past.
        var request = MultipartRequests.From(MultipartFixtures.FileThenField);

        var form = await request.StreamFilesAsync(static _ => Task.CompletedTask, Token);

        Assert.Equal("Report", form["title"]);
        Assert.Equal("z", form["tag"]);
    }

    [Fact]
    public async Task StreamFilesAsync_WhenCallbackPartiallyReadsTheFile_StillReadsLaterSections()
    {
        var request = MultipartRequests.From(MultipartFixtures.FileThenField);

        var form = await request.StreamFilesAsync(
            async file =>
            {
                var buffer = new byte[4];

                await file.OpenReadStream().ReadExactlyAsync(buffer, Token);
            },
            Token);

        Assert.Equal("Report", form["title"]);
    }

    [Fact]
    public async Task StreamFilesAsync_WithCopyToAsync_WritesTheWholeFile()
    {
        var request = MultipartRequests.From(MultipartFixtures.OneFile);
        using var target = new MemoryStream();

        await request.StreamFilesAsync(file => file.CopyToAsync(target, Token), Token);

        Assert.Equal("hello world", Encoding.UTF8.GetString(target.ToArray()));
    }
}
