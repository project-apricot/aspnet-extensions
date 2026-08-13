using System.Reflection;
using ApricotFramework.AspNetCore.Extensions.ModelBinding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace ApricotFramework.AspNetCore.Extensions.Tests.ModelBinding;

public class RequiredFromQueryTests
{
    /// <summary>
    /// Stands in for a controller action whose parameter carries the attribute.
    /// </summary>
    /// <param name="code">The parameter the conventions are read from.</param>
    /// <returns>Nothing; never invoked.</returns>
    private static string Search(string code) => code;

    /// <summary>
    /// Builds a constraint context for a request with a given query string.
    /// </summary>
    /// <param name="queryString">The raw query string, including the leading question mark.</param>
    /// <returns>The context.</returns>
    private static ActionConstraintContext CreateConstraintContext(string queryString)
    {
        var context = new DefaultHttpContext();

        context.Request.QueryString = new QueryString(queryString);

        return new ActionConstraintContext { RouteContext = new RouteContext(context) };
    }

    /// <summary>
    /// Builds a parameter model attached to an action with one selector.
    /// </summary>
    /// <param name="binderModelName">The binder model name to record, or null for none.</param>
    /// <param name="selectors">How many selectors the action should have.</param>
    /// <returns>The parameter model.</returns>
    private static ParameterModel CreateParameterModel(string? binderModelName = null, int selectors = 1)
    {
        var method = typeof(RequiredFromQueryTests).GetMethod(nameof(Search), BindingFlags.NonPublic | BindingFlags.Static)!;
        var parameterInfo = method.GetParameters()[0];

        var action = new ActionModel(method, []);

        for (var index = 0; index < selectors; index++)
        {
            action.Selectors.Add(new SelectorModel());
        }

        var parameter = new ParameterModel(parameterInfo, [])
        {
            Action = action,
            ParameterName = parameterInfo.Name!,
            BindingInfo = binderModelName is null ? null : new BindingInfo { BinderModelName = binderModelName },
        };

        return parameter;
    }

    /// <summary>
    /// Reads the single constraint the attribute attached to the last selector.
    /// </summary>
    /// <param name="parameter">The parameter the attribute was applied to.</param>
    /// <returns>The constraint.</returns>
    private static IActionConstraintMetadata SingleConstraint(ParameterModel parameter)
    {
        return Assert.Single(parameter.Action.Selectors[^1].ActionConstraints);
    }

    [Fact]
    public void Accept_WithTheKeyPresent_ReturnsTrue()
    {
        var constraint = new RequiredFromQueryActionConstraint("code");

        Assert.True(constraint.Accept(CreateConstraintContext("?code=abc")));
    }

    [Fact]
    public void Accept_WithTheKeyAbsent_ReturnsFalse()
    {
        var constraint = new RequiredFromQueryActionConstraint("code");

        Assert.False(constraint.Accept(CreateConstraintContext("?other=abc")));
    }

    [Fact]
    public void Accept_WithNoQueryAtAll_ReturnsFalse()
    {
        var constraint = new RequiredFromQueryActionConstraint("code");

        Assert.False(constraint.Accept(CreateConstraintContext(string.Empty)));
    }

    [Fact]
    public void Accept_WithTheKeyPresentButEmpty_ReturnsTrue()
    {
        // Presence is a routing question; whether the value is usable is model binding's, and
        // failing here would report a missing route rather than a bad value.
        var constraint = new RequiredFromQueryActionConstraint("code");

        Assert.True(constraint.Accept(CreateConstraintContext("?code=")));
    }

    [Fact]
    public void Accept_WithTheKeyInAnotherCase_ReturnsTrue()
    {
        // IQueryCollection compares keys case insensitively, so this matches how binding behaves.
        var constraint = new RequiredFromQueryActionConstraint("code");

        Assert.True(constraint.Accept(CreateConstraintContext("?CODE=abc")));
    }

    [Fact]
    public void Accept_WithNullContext_Throws()
    {
        var constraint = new RequiredFromQueryActionConstraint("code");

        Assert.Throws<ArgumentNullException>(() => constraint.Accept(null!));
    }

    [Fact]
    public void Accept_WithBothKeysPresent_AcceptsBothConstraints()
    {
        // Why sending both keys is a 500 rather than a choice: each constraint only adds a
        // requirement, so both are satisfied, both actions match, and routing reports an ambiguous
        // match. The documentation warns about this; this pins the mechanism behind it.
        var context = CreateConstraintContext("?id=42&query=report");

        Assert.True(new RequiredFromQueryActionConstraint("id").Accept(context));
        Assert.True(new RequiredFromQueryActionConstraint("query").Accept(context));
    }

    [Fact]
    public void Order_Always_RunsAfterTheFrameworkConstraints()
    {
        // The framework's own constraints sit at or below 100, so a value above that is what keeps
        // method and content-type matching deciding first.
        Assert.True(new RequiredFromQueryActionConstraint("code").Order > 100);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithoutParameterName_Throws(string? parameter)
    {
        Assert.ThrowsAny<ArgumentException>(() => new RequiredFromQueryActionConstraint(parameter!));
    }

    [Fact]
    public void Apply_Always_AddsOneConstraintToTheLastSelector()
    {
        var parameter = CreateParameterModel(selectors: 2);

        new RequiredFromQueryAttribute().Apply(parameter);

        Assert.Empty(parameter.Action.Selectors[0].ActionConstraints);
        Assert.IsType<RequiredFromQueryActionConstraint>(SingleConstraint(parameter));
    }

    [Fact]
    public void Apply_WithNoName_ConstrainsOnTheParameterName()
    {
        var parameter = CreateParameterModel();

        new RequiredFromQueryAttribute().Apply(parameter);

        var constraint = (RequiredFromQueryActionConstraint)SingleConstraint(parameter);

        Assert.True(constraint.Accept(CreateConstraintContext("?code=abc")));
        Assert.False(constraint.Accept(CreateConstraintContext("?renamed=abc")));
    }

    [Fact]
    public void Apply_WithBinderModelName_ConstrainsOnThatName()
    {
        var parameter = CreateParameterModel(binderModelName: "renamed");

        new RequiredFromQueryAttribute("renamed").Apply(parameter);

        var constraint = (RequiredFromQueryActionConstraint)SingleConstraint(parameter);

        Assert.True(constraint.Accept(CreateConstraintContext("?renamed=abc")));
        Assert.False(constraint.Accept(CreateConstraintContext("?code=abc")));
    }

    [Fact]
    public void Apply_WithNameButNoBindingInfo_StillConstrainsOnThatName()
    {
        // The attribute's own Name is the fallback, so the constraint is right even if the binding
        // info has not been populated from the attribute yet.
        var parameter = CreateParameterModel();

        new RequiredFromQueryAttribute("renamed").Apply(parameter);

        var constraint = (RequiredFromQueryActionConstraint)SingleConstraint(parameter);

        Assert.True(constraint.Accept(CreateConstraintContext("?renamed=abc")));
    }

    [Fact]
    public void Apply_WithNoSelectors_AddsNothing()
    {
        var parameter = CreateParameterModel(selectors: 0);

        new RequiredFromQueryAttribute().Apply(parameter);

        Assert.Empty(parameter.Action.Selectors);
    }

    [Fact]
    public void Apply_WithNullParameter_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new RequiredFromQueryAttribute().Apply(null!));
    }

    [Fact]
    public void Constructor_WithName_SetsTheBindingName()
    {
        Assert.Equal("renamed", new RequiredFromQueryAttribute("renamed").Name);
    }

    [Fact]
    public void Constructor_WithNoName_LeavesTheBindingNameUnset()
    {
        Assert.Null(new RequiredFromQueryAttribute().Name);
    }

    [Fact]
    public void Attribute_Always_BindsFromTheQueryString()
    {
        Assert.Equal(BindingSource.Query, new RequiredFromQueryAttribute().BindingSource);
    }
}
