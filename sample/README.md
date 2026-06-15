# MMLib.OpenApiForYarp — Sample

This folder demonstrates every v1 feature of the library:

| Project | What it is |
|---|---|
| `ServiceA` | Downstream **Products** API (minimal API + `Microsoft.AspNetCore.OpenApi`, Bearer scheme), `http://localhost:5101` |
| `ServiceB` | Downstream **Orders** API (with an unpublished `/internal/health`), `http://localhost:5102` |
| `Gateway` | YARP gateway using the library: per-service docs, merged document, Scalar UI, `AddOnlyPublishedPaths` on Orders, Bearer propagation, `http://localhost:5000` |
| `AppHost` | .NET Aspire orchestrator showing **service discovery** (logical names resolved before fetching downstream specs) |

## Run it (static configuration)

Start the two downstream services and the gateway (three terminals):

```bash
dotnet run --project sample/ServiceA
dotnet run --project sample/ServiceB
dotnet run --project sample/Gateway
```

Then open:

- **Scalar UI** — <http://localhost:5000/scalar> (one tab per service + an "All Services" merged tab)
- **Products document** — <http://localhost:5000/openapi/products-cluster.json> → paths rewritten to `/api/products/{id}`
- **Orders document** — <http://localhost:5000/openapi/orders-cluster.json> → `/internal/health` is filtered out (`AddOnlyPublishedPaths`)
- **Merged document** — <http://localhost:5000/openapi/all.json> → all paths, gateway-owned `info`, deduplicated `Bearer` scheme
- **Proxied API** — <http://localhost:5000/api/products/1>

## Run it (.NET Aspire — service discovery)

The Aspire AppHost runs all three projects together and addresses the downstreams by **logical name**
(`https://products-service`, `https://orders-service`). The gateway resolves those names — both for
proxying and for fetching the OpenAPI specs — via `Microsoft.Extensions.ServiceDiscovery`.

```bash
aspire run                       # uses aspire.config.json -> sample/AppHost
# or:
dotnet run --project sample/AppHost
```

This launches the Aspire dashboard and orchestrates all three projects. The same gateway code powers
both modes — only the cluster addresses differ (static localhost vs. resolved logical names).

> The AppHost targets `net10.0` and uses .NET Aspire 13. Install the Aspire CLI with
> `dotnet tool install -g aspire.cli` (or `aspire update`) and ensure the .NET 10 runtime is present.

## What to look at in the gateway

`sample/Gateway/Program.cs` is the whole integration:

```csharp
builder.Services.AddServiceDiscovery();
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver()
    .AddOpenApiForYarp();

app.MapReverseProxy();
app.MapOpenApiForYarp();   // /openapi/{cluster}.json + /openapi/all.json
app.MapScalarForYarp();    // Scalar UI at /scalar
```

`sample/Gateway/appsettings.json` holds the YARP `ReverseProxy` section and the `YarpOpenApi` section
(titles, `AddOnlyPublishedPaths`, `MergeDocuments`).
