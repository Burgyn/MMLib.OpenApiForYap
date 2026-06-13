using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MMLib.OpenApiForYarp.Fetching;

namespace MMLib.OpenApiForYarp.IntegrationTests;

/// <summary>
/// Spins up a real in-process YARP gateway (via <see cref="TestServer"/>) with the downstream
/// HTTP calls routed to a stub handler, exposing a client to issue requests against the
/// aggregated OpenAPI endpoints.
/// </summary>
internal sealed class GatewayTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private GatewayTestHost(WebApplication app, HttpClient client)
    {
        _app = app;
        Client = client;
    }

    public HttpClient Client { get; }

    public static async Task<GatewayTestHost> StartAsync(
        string configJson,
        HttpMessageHandler downstreamHandler,
        Action<IServiceCollection>? configureServices = null,
        Action<IOpenApiForYarpBuilder>? configureOpenApi = null,
        bool mapScalar = true)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(configJson)))
        {
            builder.Configuration.AddJsonStream(stream);
        }

        IOpenApiForYarpBuilder openApiBuilder = builder.Services
            .AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
            .AddOpenApiForYarp();
        configureOpenApi?.Invoke(openApiBuilder);

        builder.Services
            .AddHttpClient(DownstreamOpenApiClient.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => downstreamHandler);

        configureServices?.Invoke(builder.Services);

        WebApplication app = builder.Build();
        app.MapOpenApiForYarp();
        if (mapScalar)
        {
            app.MapScalarForYarp();
        }

        await app.StartAsync();
        return new GatewayTestHost(app, app.GetTestClient());
    }

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
        Client.Dispose();
    }
}
