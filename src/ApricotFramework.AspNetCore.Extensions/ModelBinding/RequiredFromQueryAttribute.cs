using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ApricotFramework.AspNetCore.Extensions.ModelBinding;

/// <summary>
/// Binds a parameter from the query string, and makes the action match only when that key is
/// present.
/// </summary>
/// <remarks>
/// This is how two actions on the same route are told apart by which query keys they were given.
/// Because the requirement is enforced by routing rather than validation, a request missing the key
/// matches no action and the response is <c>404 Not Found</c>, not <c>400 Bad Request</c>. Where a
/// 400 is what callers should see, leave the parameter as a plain <see cref="FromQueryAttribute"/>
/// and mark it required for validation instead.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class RequiredFromQueryAttribute : FromQueryAttribute, IParameterModelConvention
{
    /// <summary>
    /// Creates the attribute.
    /// </summary>
    /// <param name="name">
    /// The query-string key to bind from, or null to use the parameter's own name.
    /// </param>
    public RequiredFromQueryAttribute(string? name = null)
    {
        this.Name = name;
    }

    /// <summary>
    /// Attaches the presence requirement to the action the parameter belongs to.
    /// </summary>
    /// <param name="parameter">The parameter being described.</param>
    /// <remarks>
    /// The constraint goes on the last selector because that is the one describing the route this
    /// parameter was declared against. An action with no selectors is not routable at all, so there
    /// is nothing to constrain.
    /// </remarks>
    public void Apply(ParameterModel parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        if (parameter.Action.Selectors.Count == 0)
        {
            return;
        }

        var selector = parameter.Action.Selectors[^1];
        var key = parameter.BindingInfo?.BinderModelName ?? this.Name ?? parameter.ParameterName;

        selector.ActionConstraints.Add(new RequiredFromQueryActionConstraint(key));
    }
}
