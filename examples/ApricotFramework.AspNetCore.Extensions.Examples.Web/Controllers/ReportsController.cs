using ApricotFramework.AspNetCore.Extensions.ModelBinding;
using ApricotFramework.AspNetCore.Extensions.Uploads;
using Microsoft.AspNetCore.Mvc;

namespace ApricotFramework.AspNetCore.Extensions.Examples.Web.Controllers;

/// <summary>
/// Shows how two actions on one route are told apart by which query keys they were given.
/// </summary>
[ApiController]
[Route("/api/reports")]
public sealed class ReportsController : ControllerBase
{
    /// <summary>
    /// Matches only a request carrying <c>id</c>.
    /// </summary>
    /// <param name="id">The report to fetch.</param>
    /// <returns>A description of which action matched.</returns>
    [HttpGet]
    public IActionResult GetById([RequiredFromQuery] string id)
    {
        return this.Ok(new { Matched = nameof(this.GetById), Id = id });
    }

    /// <summary>
    /// Matches only a request carrying <c>query</c>, on the very same route as
    /// <see cref="GetById"/>.
    /// </summary>
    /// <param name="query">The text to search for.</param>
    /// <returns>A description of which action matched.</returns>
    /// <remarks>
    /// This is the case the attribute exists for. With plain <c>[FromQuery]</c> both actions would
    /// match every request to this route and MVC would fail as ambiguous.
    /// </remarks>
    [HttpGet]
    public IActionResult Search([RequiredFromQuery] string query)
    {
        return this.Ok(new { Matched = nameof(this.Search), Query = query });
    }

    /// <summary>
    /// Matches only a request carrying <c>slug</c>, bound from a differently named key.
    /// </summary>
    /// <param name="slug">The report to fetch.</param>
    /// <returns>A description of which action matched.</returns>
    /// <remarks>
    /// The binder name and the parameter name differ deliberately, because the constraint has to
    /// require the name on the wire rather than the name in the signature.
    /// </remarks>
    [HttpGet("by-slug")]
    public IActionResult GetBySlug([RequiredFromQuery("report-slug")] string slug)
    {
        return this.Ok(new { Matched = nameof(this.GetBySlug), Slug = slug });
    }

    /// <summary>
    /// Streams an upload from inside a controller, with form model binding turned off.
    /// </summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>What was received.</returns>
    /// <remarks>
    /// Without <see cref="DisableFormValueModelBindingAttribute"/> here, MVC would read the body to
    /// build the form collection and the stream would already be spent.
    /// </remarks>
    [HttpPost("upload")]
    [DisableFormValueModelBinding]
    public async Task<IActionResult> Upload(CancellationToken cancellationToken)
    {
        var names = new List<string>();

        var form = await this.Request.StreamFilesAsync(
            file =>
            {
                names.Add(file.FileName);

                return Task.CompletedTask;
            },
            cancellationToken);

        return this.Ok(new { Files = names, Fields = form.Keys });
    }
}
