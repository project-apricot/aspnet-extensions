using ApricotFramework.AspNetCore.Extensions.ModelBinding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace ApricotFramework.AspNetCore.Extensions.Tests.ModelBinding;

public class DisableFormValueModelBindingAttributeTests
{
    /// <summary>
    /// Builds a context carrying the factory list MVC would have assembled.
    /// </summary>
    /// <returns>The context, with every stock form and non-form factory present.</returns>
    private static ResourceExecutingContext CreateContext()
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());

        var factories = new List<IValueProviderFactory>
        {
            new FormValueProviderFactory(),
            new FormFileValueProviderFactory(),
            new JQueryFormValueProviderFactory(),
            new QueryStringValueProviderFactory(),
            new RouteValueProviderFactory(),
        };

        return new ResourceExecutingContext(actionContext, [], factories);
    }

    [Fact]
    public void OnResourceExecuting_Always_RemovesTheFormValueProviderFactory()
    {
        var context = CreateContext();

        new DisableFormValueModelBindingAttribute().OnResourceExecuting(context);

        Assert.DoesNotContain(context.ValueProviderFactories, factory => factory is FormValueProviderFactory);
    }

    [Fact]
    public void OnResourceExecuting_Always_RemovesTheFormFileValueProviderFactory()
    {
        var context = CreateContext();

        new DisableFormValueModelBindingAttribute().OnResourceExecuting(context);

        Assert.DoesNotContain(context.ValueProviderFactories, factory => factory is FormFileValueProviderFactory);
    }

    [Fact]
    public void OnResourceExecuting_Always_RemovesTheJQueryFormValueProviderFactory()
    {
        var context = CreateContext();

        new DisableFormValueModelBindingAttribute().OnResourceExecuting(context);

        Assert.DoesNotContain(context.ValueProviderFactories, factory => factory is JQueryFormValueProviderFactory);
    }

    [Fact]
    public void OnResourceExecuting_Always_LeavesNonFormFactoriesAlone()
    {
        // Removing more than the form factories would silently break route and query binding on the
        // very actions that need this attribute.
        var context = CreateContext();

        new DisableFormValueModelBindingAttribute().OnResourceExecuting(context);

        Assert.Equal(2, context.ValueProviderFactories.Count);
        Assert.Contains(context.ValueProviderFactories, factory => factory is QueryStringValueProviderFactory);
        Assert.Contains(context.ValueProviderFactories, factory => factory is RouteValueProviderFactory);
    }

    [Fact]
    public void OnResourceExecuting_WithNoFormFactories_ChangesNothing()
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        var factories = new List<IValueProviderFactory> { new QueryStringValueProviderFactory() };
        var context = new ResourceExecutingContext(actionContext, [], factories);

        new DisableFormValueModelBindingAttribute().OnResourceExecuting(context);

        Assert.Single(context.ValueProviderFactories);
    }

    [Fact]
    public void OnResourceExecuting_WithNullContext_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DisableFormValueModelBindingAttribute().OnResourceExecuting(null!));
    }

    [Fact]
    public void OnResourceExecuted_WithNullContext_DoesNotThrow()
    {
        // Nothing is restored, so there is nothing to dereference either.
        new DisableFormValueModelBindingAttribute().OnResourceExecuted(null!);
    }

    [Fact]
    public void Attribute_Always_TargetsClassesAndMethods()
    {
        var usage = Assert.Single(
            typeof(DisableFormValueModelBindingAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false));

        Assert.Equal(
            AttributeTargets.Class | AttributeTargets.Method,
            ((AttributeUsageAttribute)usage).ValidOn);
    }
}
