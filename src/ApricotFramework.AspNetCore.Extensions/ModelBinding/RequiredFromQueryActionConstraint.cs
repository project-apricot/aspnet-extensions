using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace ApricotFramework.AspNetCore.Extensions.ModelBinding;

/// <summary>
/// An action constraint that accepts a request only when it carries a given query-string key.
/// </summary>
public sealed class RequiredFromQueryActionConstraint : IActionConstraint
{
    /// <summary>
    /// Runs after the constraints the framework applies itself, which use orders at and below 100.
    /// Method and content-type matching therefore decide first, and this only narrows a candidate
    /// that already matched.
    /// </summary>
    private const int LateOrder = 999;

    /// <summary>
    /// The query-string key that must be present.
    /// </summary>
    private readonly string parameter;

    /// <summary>
    /// Creates a constraint for one query-string key.
    /// </summary>
    /// <param name="parameter">The key that must be present.</param>
    public RequiredFromQueryActionConstraint(string parameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameter);

        this.parameter = parameter;
    }

    /// <inheritdoc />
    public int Order => LateOrder;

    /// <inheritdoc />
    /// <remarks>
    /// Only presence is checked, so <c>?key=</c> satisfies the constraint. Validating the value is
    /// model binding's job, and rejecting an empty one here would report a missing route rather
    /// than a bad value.
    /// </remarks>
    public bool Accept(ActionConstraintContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.RouteContext.HttpContext.Request.Query.ContainsKey(this.parameter);
    }
}
