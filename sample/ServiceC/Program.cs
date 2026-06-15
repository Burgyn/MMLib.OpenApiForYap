using Sample.Common;
using ServiceC.Customers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddSampleOpenApi(
    title: "Customers API",
    description: "Customer profiles, loyalty tiers and addresses.",
    includeApiKey: true);

var app = builder.Build();

app.MapOpenApi(); // serves /openapi/v1.json
app.MapCustomers();

app.Run();
