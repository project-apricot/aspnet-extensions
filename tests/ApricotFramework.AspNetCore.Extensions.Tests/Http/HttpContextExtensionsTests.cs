using ApricotFramework.AspNetCore.Extensions.Http;
using Microsoft.AspNetCore.Http;

namespace ApricotFramework.AspNetCore.Extensions.Tests.Http;

public class HttpContextExtensionsTests
{
    /// <summary>
    /// A type stored in <see cref="HttpContext.Items"/> by the tests.
    /// </summary>
    private sealed class Tenant
    {
        /// <summary>
        /// Something to compare on.
        /// </summary>
        public string Name { get; init; } = string.Empty;
    }

    /// <summary>
    /// An unrelated type stored under the same key, standing in for other middleware.
    /// </summary>
    private sealed class Session
    {
    }

    /// <summary>
    /// Builds a context whose request carries a query string.
    /// </summary>
    /// <param name="queryString">The raw query string, including the leading question mark.</param>
    /// <returns>The context.</returns>
    private static DefaultHttpContext CreateContext(string queryString)
    {
        var context = new DefaultHttpContext();

        context.Request.QueryString = new QueryString(queryString);

        return context;
    }

    [Fact]
    public void ToValuesDictionary_WithSingleValue_CollapsesToTheValue()
    {
        var values = CreateContext("?code=abc").Request.Query.ToValuesDictionary();

        Assert.Equal("abc", values["code"]);
    }

    [Fact]
    public void ToValuesDictionary_WithRepeatedKey_ReturnsAList()
    {
        var values = CreateContext("?scope=read&scope=write").Request.Query.ToValuesDictionary();

        Assert.Equal(["read", "write"], Assert.IsType<List<string>>(values["scope"]));
    }

    [Fact]
    public void ToValuesDictionary_WithEmptyValue_ReturnsEmptyString()
    {
        var values = CreateContext("?code=").Request.Query.ToValuesDictionary();

        Assert.Equal(string.Empty, values["code"]);
    }

    [Fact]
    public void ToValuesDictionary_WithValuelessKey_ReturnsEmptyString()
    {
        var values = CreateContext("?flag").Request.Query.ToValuesDictionary();

        Assert.Equal(string.Empty, values["flag"]);
    }

    [Fact]
    public void ToValuesDictionary_WithNoQuery_ReturnsEmpty()
    {
        var values = CreateContext(string.Empty).Request.Query.ToValuesDictionary();

        Assert.Empty(values);
    }

    [Fact]
    public void ToValuesDictionary_WithSeveralKeys_ReturnsEachOne()
    {
        var values = CreateContext("?a=1&b=2&c=3").Request.Query.ToValuesDictionary();

        Assert.Equal(3, values.Count);
        Assert.Equal("1", values["a"]);
        Assert.Equal("3", values["c"]);
    }

    [Fact]
    public void ToValuesDictionary_WithKeysDifferingInCase_MergesThemAsTheRequestDoes()
    {
        // IQueryCollection is itself case insensitive, so these two spellings have already become
        // one key with two values before this method ever sees them.
        var values = CreateContext("?Code=upper&code=lower").Request.Query.ToValuesDictionary();

        Assert.Single(values);
        Assert.Equal(["upper", "lower"], Assert.IsType<List<string>>(values.Values.Single()));
    }

    [Fact]
    public void ToValuesDictionary_Always_LooksKeysUpCaseInsensitively()
    {
        // Matching the source collection's comparer: a dictionary stricter than the request it came
        // from would answer differently from query["Code"] for no visible reason.
        var values = CreateContext("?code=abc").Request.Query.ToValuesDictionary();

        Assert.Equal("abc", values["Code"]);
    }

    [Fact]
    public void ToValuesDictionary_WithUrlEncodedValue_ReturnsItDecoded()
    {
        var values = CreateContext("?name=caf%C3%A9%20bar").Request.Query.ToValuesDictionary();

        Assert.Equal("café bar", values["name"]);
    }

    [Fact]
    public void ToValuesDictionary_WithNullQuery_Throws()
    {
        IQueryCollection query = null!;

        Assert.Throws<ArgumentNullException>(query.ToValuesDictionary);
    }

    [Fact]
    public void GetItemOrDefault_WithStoredItem_ReturnsIt()
    {
        var context = new DefaultHttpContext();
        var tenant = new Tenant { Name = "acme" };

        context.Items["tenant"] = tenant;

        Assert.Same(tenant, context.GetItemOrDefault<Tenant>("tenant", null));
    }

    [Fact]
    public void GetItemOrDefault_WithMissingItem_ReturnsTheDefault()
    {
        var fallback = new Tenant { Name = "fallback" };

        Assert.Same(fallback, new DefaultHttpContext().GetItemOrDefault("tenant", fallback));
    }

    [Fact]
    public void GetItemOrDefault_WithItemOfAnotherType_ReturnsTheDefault()
    {
        // The old implementation cast unconditionally and threw InvalidCastException here, so one
        // component's choice of key could crash another's read.
        var context = new DefaultHttpContext();
        var fallback = new Tenant { Name = "fallback" };

        context.Items["tenant"] = new Session();

        Assert.Same(fallback, context.GetItemOrDefault("tenant", fallback));
    }

    [Fact]
    public void GetItemOrDefault_WithNullStoredItem_ReturnsTheDefault()
    {
        // The old implementation returned the stored null while its signature promised non-null.
        var context = new DefaultHttpContext();
        var fallback = new Tenant { Name = "fallback" };

        context.Items["tenant"] = null;

        Assert.Same(fallback, context.GetItemOrDefault("tenant", fallback));
    }

    [Fact]
    public void GetItemOrDefault_WithNullDefaultAndMissingItem_ReturnsNull()
    {
        Assert.Null(new DefaultHttpContext().GetItemOrDefault<Tenant>("tenant", null));
    }

    [Fact]
    public void GetItemOrDefault_WithNullContext_Throws()
    {
        HttpContext context = null!;

        Assert.Throws<ArgumentNullException>(() => context.GetItemOrDefault<Tenant>("tenant", null));
    }

    [Fact]
    public void GetItemOrDefault_WithNullKey_Throws()
    {
        var context = new DefaultHttpContext();

        Assert.Throws<ArgumentNullException>(() => context.GetItemOrDefault<Tenant>(null!, null));
    }
}
