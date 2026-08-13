using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ApricotFramework.AspNetCore.Extensions.Uploads;

/// <summary>
/// Extensions for binding a form collection that was read outside model binding.
/// </summary>
public static class FormCollectionExtensions
{
    /// <summary>
    /// Presents form fields as a value provider, so a model can be bound from the fields that
    /// arrived alongside a streamed upload.
    /// </summary>
    /// <param name="form">The fields to expose.</param>
    /// <param name="culture">The culture used to convert values to model types.</param>
    /// <returns>A value provider over the fields.</returns>
    /// <remarks>
    /// The culture is deliberately not optional. Pass <see cref="CultureInfo.CurrentCulture"/> to
    /// match what MVC's own form value provider does, or
    /// <see cref="CultureInfo.InvariantCulture"/> when the fields are protocol data rather than
    /// something a person typed — a decimal or a date sent by a client should not be read
    /// differently depending on where the server happens to run.
    /// </remarks>
    public static IValueProvider ToValueProvider(this IFormCollection form, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(culture);

        return new FormValueProvider(BindingSource.Form, form, culture);
    }
}
