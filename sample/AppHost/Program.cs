// .NET Aspire orchestrator. Runs the two downstream services and the YARP gateway, wiring service
// discovery so the gateway addresses downstreams by logical name (https://products-service, etc.).
// The gateway resolves those logical names — both for proxying and for fetching OpenAPI documents —
// via Microsoft.Extensions.ServiceDiscovery.
var builder = DistributedApplication.CreateBuilder(args);

var products = builder.AddProject<Projects.ServiceA>("products-service");
var orders = builder.AddProject<Projects.ServiceB>("orders-service");

builder.AddProject<Projects.Gateway>("gateway")
    .WithReference(products)
    .WithReference(orders)
    // Override the static localhost cluster addresses with logical service-discovery names.
    .WithEnvironment("ReverseProxy__Clusters__products-cluster__Destinations__default__Address", "https://products-service")
    .WithEnvironment("ReverseProxy__Clusters__orders-cluster__Destinations__default__Address", "https://orders-service");

builder.Build().Run();
