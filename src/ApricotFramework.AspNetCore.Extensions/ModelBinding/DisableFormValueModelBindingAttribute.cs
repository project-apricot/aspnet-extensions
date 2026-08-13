using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ApricotFramework.AspNetCore.Extensions.ModelBinding;

/// <summary>
/// Stops MVC from reading the request body as a form, leaving it intact for the action to stream.
/// </summary>
/// <remarks>
/// Required on any action that reads a multipart body itself. Model binding buffers the whole
/// body to populate the form collection, and once it has, the body cannot be read a second time —
/// so a streaming action without this attribute finds nothing there.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
{
    /// <summary>
    /// Removes the form-reading value provider factories before model binding runs.
    /// </summary>
    /// <param name="context">The context whose factories to trim.</param>
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var factories = context.ValueProviderFactories;

        factories.RemoveType<FormValueProviderFactory>();
        factories.RemoveType<FormFileValueProviderFactory>();
        factories.RemoveType<JQueryFormValueProviderFactory>();
    }

    /// <summary>
    /// Does nothing.
    /// </summary>
    /// <param name="context">The context of the completed action.</param>
    /// <remarks>
    /// Nothing needs restoring: the factory list is built per request, so the removal above cannot
    /// leak into another one.
    /// </remarks>
    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
