using ApricotFramework.AspNetCore.Extensions.Configuration;
using Microsoft.Extensions.Configuration;

namespace ApricotFramework.AspNetCore.Extensions.Tests.Configuration;

public class ConfigurationBindingExtensionsTests
{
    /// <summary>
    /// A settings shape with a default, a nested object and a collection.
    /// </summary>
    private sealed class StorageSettings
    {
        /// <summary>
        /// A value with a default, so an unbound instance is distinguishable from a bound one.
        /// </summary>
        public string Bucket { get; set; } = "default-bucket";

        /// <summary>
        /// A numeric value, to prove type conversion happens.
        /// </summary>
        public int MaxSizeMb { get; set; }

        /// <summary>
        /// A nested section.
        /// </summary>
        public CredentialSettings Credentials { get; set; } = new();

        /// <summary>
        /// A repeated key.
        /// </summary>
        public IList<string> Regions { get; } = [];
    }

    /// <summary>
    /// The nested part of <see cref="StorageSettings"/>.
    /// </summary>
    private sealed class CredentialSettings
    {
        /// <summary>
        /// A nested value.
        /// </summary>
        public string? AccessKey { get; set; }
    }

    /// <summary>
    /// Builds a configuration from in-memory values.
    /// </summary>
    /// <param name="values">The configuration entries.</param>
    /// <returns>The configuration.</returns>
    private static IConfiguration CreateConfiguration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)))
            .Build();
    }

    [Fact]
    public void BindAs_WithMatchingSection_BindsScalarValues()
    {
        var configuration = CreateConfiguration(
            ("Storage:Bucket", "uploads"),
            ("Storage:MaxSizeMb", "512"));

        var settings = configuration.BindAs<StorageSettings>("Storage");

        Assert.Equal("uploads", settings.Bucket);
        Assert.Equal(512, settings.MaxSizeMb);
    }

    [Fact]
    public void BindAs_WithNestedSection_BindsNestedValues()
    {
        var configuration = CreateConfiguration(("Storage:Credentials:AccessKey", "AKIA"));

        var settings = configuration.BindAs<StorageSettings>("Storage");

        Assert.Equal("AKIA", settings.Credentials.AccessKey);
    }

    [Fact]
    public void BindAs_WithIndexedKeys_BindsCollections()
    {
        var configuration = CreateConfiguration(
            ("Storage:Regions:0", "eu-west-1"),
            ("Storage:Regions:1", "us-east-2"));

        var settings = configuration.BindAs<StorageSettings>("Storage");

        Assert.Equal(["eu-west-1", "us-east-2"], settings.Regions);
    }

    [Fact]
    public void BindAs_WithMissingSection_ReturnsDefaults()
    {
        var settings = CreateConfiguration().BindAs<StorageSettings>("Storage");

        Assert.Equal("default-bucket", settings.Bucket);
        Assert.Equal(0, settings.MaxSizeMb);
        Assert.Empty(settings.Regions);
    }

    [Fact]
    public void BindAs_WithMissingSection_ReturnsNewInstanceEachCall()
    {
        var configuration = CreateConfiguration(("Storage:Bucket", "uploads"));

        var first = configuration.BindAs<StorageSettings>("Storage");
        var second = configuration.BindAs<StorageSettings>("Storage");

        Assert.NotSame(first, second);
    }

    [Fact]
    public void BindAs_WithUnrelatedSection_LeavesDefaults()
    {
        var configuration = CreateConfiguration(("Other:Bucket", "uploads"));

        var settings = configuration.BindAs<StorageSettings>("Storage");

        Assert.Equal("default-bucket", settings.Bucket);
    }

    [Fact]
    public void BindAs_WithNullConfiguration_Throws()
    {
        IConfiguration configuration = null!;

        Assert.Throws<ArgumentNullException>(() => configuration.BindAs<StorageSettings>("Storage"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BindAs_WithoutSectionName_Throws(string? name)
    {
        var configuration = CreateConfiguration();

        Assert.ThrowsAny<ArgumentException>(() => configuration.BindAs<StorageSettings>(name!));
    }
}
