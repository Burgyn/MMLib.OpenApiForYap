using Sample.Common;
using ServiceB.Ordering;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddSampleOpenApi(
    title: "Orders API",
    description: "Customer orders, line items and fulfillment status.");

var app = builder.Build();

app.MapOpenApi(); // serves /openapi/v1.json
app.MapOrders();

// An internal endpoint that is NOT proxied by the gateway — demonstrates AddOnlyPublishedPaths.
app.MapGet("/internal/health", () => TypedResults.Ok(new { status = "healthy" })).WithTags("Internal");

app.Run();
