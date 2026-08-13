using System.Globalization;
using ApricotFramework.AspNetCore.Extensions.Uploads;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace ApricotFramework.AspNetCore.Extensions.Tests.Uploads;

public class FormCollectionExtensionsTests
{
    /// <summary>
    /// Builds a form collection from key and value pairs.
    /// </summary>
    /// <param name="values">The fields to expose.</param>
    /// <returns>The collection.</returns>
    private static FormCollection CreateForm(params (string Key, string Value)[] values)
    {
        return new FormCollection(values.ToDictionary(
            pair => pair.Key,
            pair => new StringValues(pair.Value),
            StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void ToValueProvider_WithKnownKey_ContainsThatPrefix()
    {
        var provider = CreateForm(("title", "Report")).ToValueProvider(CultureInfo.InvariantCulture);

        Assert.True(provider.ContainsPrefix("title"));
    }

    [Fact]
    public void ToValueProvider_WithKnownKey_ReturnsTheValue()
    {
        var provider = CreateForm(("title", "Report")).ToValueProvider(CultureInfo.InvariantCulture);

        Assert.Equal("Report", provider.GetValue("title").FirstValue);
    }

    [Fact]
    public void ToValueProvider_WithUnknownKey_ReturnsNone()
    {
        var provider = CreateForm(("title", "Report")).ToValueProvider(CultureInfo.InvariantCulture);

        Assert.Equal(ValueProviderResult.None, provider.GetValue("missing"));
    }

    [Fact]
    public void ToValueProvider_WithCulture_RecordsThatCulture()
    {
        var culture = CultureInfo.GetCultureInfo("de-DE");

        var provider = CreateForm(("total", "1,5")).ToValueProvider(culture);

        Assert.Equal(culture, provider.GetValue("total").Culture);
    }

    [Fact]
    public void ToValueProvider_WithInvariantCulture_RecordsInvariant()
    {
        var provider = CreateForm(("total", "1.5")).ToValueProvider(CultureInfo.InvariantCulture);

        Assert.Equal(CultureInfo.InvariantCulture, provider.GetValue("total").Culture);
    }

    [Fact]
    public void ToValueProvider_WithCurrentCulture_MatchesWhatMvcWouldUse()
    {
        var provider = CreateForm(("total", "1.5")).ToValueProvider(CultureInfo.CurrentCulture);

        Assert.Equal(CultureInfo.CurrentCulture, provider.GetValue("total").Culture);
    }

    [Fact]
    public void ToValueProvider_WithEmptyForm_ContainsNothing()
    {
        var provider = FormCollection.Empty.ToValueProvider(CultureInfo.InvariantCulture);

        Assert.False(provider.ContainsPrefix("anything"));
    }

    [Fact]
    public void ToValueProvider_WithNullForm_Throws()
    {
        IFormCollection form = null!;

        Assert.Throws<ArgumentNullException>(() => form.ToValueProvider(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ToValueProvider_WithNullCulture_Throws()
    {
        var form = CreateForm(("title", "Report"));

        Assert.Throws<ArgumentNullException>(() => form.ToValueProvider(null!));
    }
}
