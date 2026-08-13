using System.Globalization;
using ApricotFramework.AspNetCore.Extensions.Configuration;
using ApricotFramework.AspNetCore.Extensions.Cookies;
using ApricotFramework.AspNetCore.Extensions.Cors;
using ApricotFramework.AspNetCore.Extensions.Forwarding;
using ApricotFramework.AspNetCore.Extensions.Examples.Web;
using ApricotFramework.AspNetCore.Extensions.Http;
using ApricotFramework.AspNetCore.Extensions.Uploads;

var builder = WebApplication.CreateBuilder(args);

// A detached snapshot of the "Storage" section, bound in one call. Nothing is registered in the
// container, which is the point: this is for code that just needs the values now.
var storage = builder.Configuration.BindAs<StorageSettings>("Storage");

// This library puts no ceiling on an upload, but Kestrel caps a request body at 30 MB by default,
// and that cap has to be raised here: unlike endpoints and certificates, Limits is not bound from
// the "Kestrel" configuration section, so setting it in appsettings.json does nothing.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = storage.MaxSizeMb * 1024L * 1024L;
});

builder.Services.AddAnyOriginCors();
builder.Services.AddSecureCookiePolicy();
builder.Services.AddForwardingConfiguration();
builder.Services.AddControllers();

var app = builder.Build();

// Order matters: forwarded headers must be applied before anything reads the scheme or the client
// address, and the cookie policy before anything writes a cookie.
app.UseForwardedHeaders();
app.UseCookiePolicy();
app.UseCors(CorsExtensions.DefaultPolicyName);

// Shows what BindAs produced, so a change in appsettings.json is visible immediately.
app.MapGet("/settings", () => Results.Ok(storage));

// Reports what the forwarded-header middleware concluded. Behind a proxy these come from the
// X-Forwarded-* headers; sent directly they come from the connection.
app.MapGet("/whoami", (HttpContext context) => Results.Ok(new
{
    context.Request.Scheme,
    Host = context.Request.Host.Value,
    RemoteAddress = context.Connection.RemoteIpAddress?.ToString(),
}));

// Flattens an arbitrary query string, collapsing single-valued keys and keeping repeated ones as
// lists. Try /echo?a=1&b=2&b=3&flag.
app.MapGet("/echo", (HttpContext context) => Results.Ok(context.Request.Query.ToValuesDictionary()));

// Writes a cookie the policy then adjusts: Secure is added even though this asks for it to be off.
app.MapGet("/set-cookie", (HttpContext context) =>
{
    context.Response.Cookies.Append("session", "value", new CookieOptions
    {
        SameSite = SameSiteMode.None,
        Secure = false,
    });

    return Results.Ok(new { Wrote = "session", Sent = context.Response.Headers.SetCookie.ToString() });
});

// Reads a per-request item, including the case that used to throw: an item stored under the same
// key by something else entirely.
app.MapGet("/items", (HttpContext context) =>
{
    context.Items["tenant"] = new Tenant("acme");
    context.Items["clash"] = "a string, not a Tenant";

    return Results.Ok(new
    {
        Found = context.GetItemOrDefault<Tenant>("tenant", null)?.Name,
        WrongType = context.GetItemOrDefault("clash", new Tenant("fallback")).Name,
        Missing = context.GetItemOrDefault("absent", new Tenant("fallback")).Name,
    });
});

// The streaming upload. Nothing is buffered: each file is counted as it arrives, so the memory used
// does not grow with the request. curl -F 'file=@big.bin' -F 'title=Report' http://localhost:5160/upload
app.MapPost("/upload", async (HttpRequest request, CancellationToken cancellationToken) =>
{
    var files = new List<object>();

    var form = await request.StreamFilesAsync(
        async file =>
        {
            // Counting rather than collecting: this is what makes the endpoint indifferent to size.
            var bytes = await CountBytesAsync(file, cancellationToken);

            files.Add(new
            {
                file.Name,
                file.FileName,
                file.ContentType,
                ReportedLength = file.Length,
                ActualBytes = bytes,
            });
        },
        cancellationToken);

    // Invariant, not current culture: these values came off the wire, not from a keyboard.
    var values = form.ToValueProvider(CultureInfo.InvariantCulture);

    return Results.Ok(new
    {
        Files = files,
        Fields = form.Keys.ToDictionary(key => key, key => form[key].ToArray()),
        TitleViaValueProvider = values.GetValue("title").FirstValue,
    });
});

app.MapControllers();

app.Run();

/// <summary>
/// Reads a streamed file to its end without holding it.
/// </summary>
/// <param name="file">The file being streamed.</param>
/// <param name="cancellationToken">Cancels the read.</param>
/// <returns>The number of bytes read.</returns>
static async Task<long> CountBytesAsync(IFormFile file, CancellationToken cancellationToken)
{
    var buffer = new byte[81920];
    var stream = file.OpenReadStream();
    long total = 0;

    while (true)
    {
        var read = await stream.ReadAsync(buffer, cancellationToken);

        if (read == 0)
        {
            return total;
        }

        total += read;
    }
}
