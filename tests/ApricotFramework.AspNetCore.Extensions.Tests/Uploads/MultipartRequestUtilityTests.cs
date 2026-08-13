using System.Text;
using ApricotFramework.AspNetCore.Extensions.Uploads;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace ApricotFramework.AspNetCore.Extensions.Tests.Uploads;

public class MultipartRequestUtilityTests
{
    /// <summary>
    /// Builds a section declaring a given content type.
    /// </summary>
    /// <param name="contentType">The content type to declare, or null for none.</param>
    /// <returns>The section.</returns>
    private static MultipartSection CreateSection(string? contentType)
    {
        var headers = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);

        if (contentType is not null)
        {
            headers[HeaderNames.ContentType] = contentType;
        }

        return new MultipartSection { Body = new MemoryStream(), Headers = headers };
    }

    [Theory]
    [InlineData("multipart/form-data; boundary=abc")]
    [InlineData("multipart/mixed; boundary=abc")]
    [InlineData("MULTIPART/FORM-DATA; boundary=abc")]
    public void IsMultipartContentType_WithMultipartMediaType_ReturnsTrue(string contentType)
    {
        Assert.True(MultipartRequestUtility.IsMultipartContentType(contentType));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("application/json")]
    [InlineData("text/plain")]
    public void IsMultipartContentType_WithOtherMediaType_ReturnsFalse(string? contentType)
    {
        Assert.False(MultipartRequestUtility.IsMultipartContentType(contentType));
    }

    [Theory]
    [InlineData("text/plain; note=multipart/form-data")]
    [InlineData("application/x-multipart/form-data")]
    public void IsMultipartContentType_WithMultipartMentionedLater_ReturnsFalse(string contentType)
    {
        // The old check searched the whole value, so both of these were treated as multipart.
        Assert.False(MultipartRequestUtility.IsMultipartContentType(contentType));
    }

    [Fact]
    public void GetBoundary_WithUnquotedBoundary_ReturnsIt()
    {
        var contentType = MediaTypeHeaderValue.Parse("multipart/form-data; boundary=simple");

        Assert.Equal("simple", MultipartRequestUtility.GetBoundary(contentType, 70));
    }

    [Fact]
    public void GetBoundary_WithQuotedBoundary_StripsTheQuotes()
    {
        var contentType = MediaTypeHeaderValue.Parse("multipart/form-data; boundary=\"----WebKitFormBoundary123\"");

        Assert.Equal("----WebKitFormBoundary123", MultipartRequestUtility.GetBoundary(contentType, 70));
    }

    [Fact]
    public void GetBoundary_WithNoBoundary_Throws()
    {
        var contentType = MediaTypeHeaderValue.Parse("multipart/form-data");

        var exception = Assert.Throws<InvalidDataException>(() => MultipartRequestUtility.GetBoundary(contentType, 70));

        Assert.Contains("Missing content-type boundary", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetBoundary_WithEmptyBoundary_Throws()
    {
        var contentType = MediaTypeHeaderValue.Parse("multipart/form-data; boundary=\"\"");

        Assert.Throws<InvalidDataException>(() => MultipartRequestUtility.GetBoundary(contentType, 70));
    }

    [Fact]
    public void GetBoundary_WithBoundaryOverTheLimit_Throws()
    {
        var contentType = MediaTypeHeaderValue.Parse($"multipart/form-data; boundary={new string('b', 71)}");

        var exception = Assert.Throws<InvalidDataException>(() => MultipartRequestUtility.GetBoundary(contentType, 70));

        Assert.Contains("length limit 70", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetBoundary_WithBoundaryExactlyAtTheLimit_ReturnsIt()
    {
        var boundary = new string('b', 70);
        var contentType = MediaTypeHeaderValue.Parse($"multipart/form-data; boundary={boundary}");

        Assert.Equal(boundary, MultipartRequestUtility.GetBoundary(contentType, 70));
    }

    [Fact]
    public void GetBoundary_WithNullContentType_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => MultipartRequestUtility.GetBoundary(null!, 70));
    }

    [Fact]
    public void GetEncoding_WithDeclaredCharset_ReturnsIt()
    {
        var section = CreateSection("text/plain; charset=iso-8859-1");

        Assert.Equal(Encoding.Latin1, MultipartRequestUtility.GetEncoding(section));
    }

    [Fact]
    public void GetEncoding_WithContentTypeButNoCharset_ReturnsUtf8()
    {
        // This is the case the old implementation got wrong: the parse succeeds, Encoding is null,
        // and returning it made the caller throw a NullReferenceException.
        var section = CreateSection("text/plain");

        Assert.Equal(Encoding.UTF8, MultipartRequestUtility.GetEncoding(section));
    }

    [Fact]
    public void GetEncoding_WithNoContentType_ReturnsUtf8()
    {
        var section = CreateSection(contentType: null);

        Assert.Equal(Encoding.UTF8, MultipartRequestUtility.GetEncoding(section));
    }

    [Fact]
    public void GetEncoding_WithUnparseableContentType_ReturnsUtf8()
    {
        var section = CreateSection("this is not a media type");

        Assert.Equal(Encoding.UTF8, MultipartRequestUtility.GetEncoding(section));
    }

    [Fact]
    public void GetEncoding_WithNullSection_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => MultipartRequestUtility.GetEncoding(null!));
    }
}
