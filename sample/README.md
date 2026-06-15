# MMLib.OpenApiForYarp — Sample

This folder is a small e-commerce gateway that demonstrates every v1 feature of the library across
**three** downstream services. The services are `Microsoft.AspNetCore.OpenApi` minimal APIs with
production-looking specs — paged list endpoints with filtering/sorting, nested resources, typed
responses + RFC 7807 `ProblemDetails`, DataAnnotations-driven schema constraints, enums, and
per-write-operation security requirements. Shared OpenAPI/paging/validation helpers live in
`Sample.Common`.

| Project | What it is |
|---|---|
| `ServiceA` | **Products** API — products CRUD, price patch, categories, nested category products. Bearer. `http://localhost:5101` |
| `ServiceB` | **Orders** API — orders list/get/create, `cancel` (409 on terminal state), nested line items, plus an unpublished `/internal/health`. Bearer. `http://localhost:5102` |
| `ServiceC` | **Customers** API — customers CRUD, loyalty tiers, nested addresses. **Bearer + ApiKey**. `http://localhost:5103` |
| `Gateway` | YARP gateway using the library: per-service docs, merged document, Scalar UI, `AddOnlyPublishedPaths` on Orders, security propagation. `http://localhost:5080` |
| `Sample.Common` | Shared OpenAPI document config (info, security schemes, write-op security), `PagedResult<T>`, problem-details and validation helpers |
| `AppHost` | .NET Aspire orchestrator showing **service discovery** (logical names resolved before fetching downstream specs) |

## Run it (static configuration)

Start the three downstream services and the gateway (four terminals):

```bash
dotnet run --project sample/ServiceA
dotnet run --project sample/ServiceB
dotnet run --project sample/ServiceC
dotnet run --project sample/Gateway
```

Then open:

- **Scalar UI** — <http://localhost:5080/scalar> (one tab per service + an "All Services" merged tab)
- **Products** — <http://localhost:5080/openapi/products-cluster.json> → paths rewritten to `/api/products/{id}`, write ops require Bearer
- **Orders** — <http://localhost:5080/openapi/orders-cluster.json> → `/internal/health` is filtered out (`AddOnlyPublishedPaths`)
- **Customers** — <http://localhost:5080/openapi/customers-cluster.json> → exposes both `Bearer` and `ApiKey` schemes
- **Merged** — <http://localhost:5080/openapi/all.json> → all 13 paths, gateway-owned `info`, security schemes deduplicated to `Bearer` + `ApiKey`
- **Proxied APIs** — <http://localhost:5080/api/products>, <http://localhost:5080/api/orders>, <http://localhost:5080/api/customers>

## Run it (.NET Aspire — service discovery)

The Aspire AppHost runs all four projects together and addresses the downstreams by **logical name**
(`http://products-service`, `http://orders-service`, `http://customers-service`). The gateway resolves those names — both for
proxying and for fetching the OpenAPI specs — via `Microsoft.Extensions.ServiceDiscovery`. Ports are
assigned by Aspire (don't hard-code `Urls` in a project that Aspire orchestrates).

```bash
aspire run                       # uses aspire.config.json -> sample/AppHost
# or:
dotnet run --project sample/AppHost
```

This launches the Aspire dashboard and orchestrates all four projects. The same gateway code powers
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
