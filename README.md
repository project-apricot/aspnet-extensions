# ApricotFramework.AspNetCore.Extensions

[![NuGet](https://img.shields.io/nuget/v/ApricotFramework.AspNetCore.Extensions.svg?label=ApricotFramework.AspNetCore.Extensions)](https://www.nuget.org/packages/ApricotFramework.AspNetCore.Extensions/)
[![CI](https://github.com/project-apricot/aspnet-extensions/actions/workflows/ci.yml/badge.svg)](https://github.com/project-apricot/aspnet-extensions/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/project-apricot/aspnet-extensions/blob/main/LICENSE)

General-purpose ASP.NET Core extensions: streaming multipart uploads, query-string-aware routing,
and one-line setup for CORS, forwarded headers and cookie policy.

No package dependencies — just a `FrameworkReference` to `Microsoft.AspNetCore.App`.

## Install

```bash
dotnet add package ApricotFramework.AspNetCore.Extensions
```

## Usage

```csharp
using ApricotFramework.AspNetCore.Extensions.Cors;
using ApricotFramework.AspNetCore.Extensions.Forwarding;

builder.Services.AddAnyOriginCors();
builder.Services.AddForwardingConfiguration();

app.UseForwardedHeaders();
app.UseCors(CorsExtensions.DefaultPolicyName);
```

Read an upload without buffering it. Each file is handed over as it arrives, so memory use does not
grow with the request — 1 GB costs no more than 1 MB:

```csharp
using ApricotFramework.AspNetCore.Extensions.ModelBinding;
using ApricotFramework.AspNetCore.Extensions.Uploads;

[HttpPost("upload")]
[DisableFormValueModelBinding]                  // or MVC reads the body first and leaves nothing
public async Task<IActionResult> Upload(CancellationToken cancellationToken)
{
    var fields = await this.Request.StreamFilesAsync(
        file => blobStore.WriteAsync(file.FileName, file.OpenReadStream(), cancellationToken),
        cancellationToken);

    return this.Ok(new { Title = fields["title"].ToString() });
}
```

This library sets no size limit, but **Kestrel caps a request body at 30 MB** by default and answers
`413` above it. Raise it in code — `Limits` is not read from the `Kestrel` configuration section, so
setting it in `appsettings.json` is silently ignored:

```csharp
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 2048L * 1024 * 1024);
```

Note that `AddForwardingConfiguration` trusts `X-Forwarded-*` from **any** peer, because a proxy's
address is rarely knowable up front. That is safe only while the application cannot be reached
except through the proxy — anything able to connect directly can otherwise choose its own apparent
IP address, scheme and host. Pass a callback to name the real proxies where the deployment allows
it.

Full documentation: <https://projectapricot.dev>
