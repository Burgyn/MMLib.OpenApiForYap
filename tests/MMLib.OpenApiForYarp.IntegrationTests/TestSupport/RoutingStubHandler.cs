using System.Net;
using System.Text;

namespace MMLib.OpenApiForYarp.IntegrationTests;

/// <summary>
/// Stub <see cref="HttpMessageHandler"/> that returns a fixture OpenAPI JSON based on the request
/// authority (host:port), standing in for downstream services during integration tests.
/// </summary>
internal sealed class RoutingStubHandler(IReadOnlyDictionary<string, string> jsonByAuthority) : HttpMessageHandler
{
    private int _callCount;

    public int CallCount => _callCount;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _callCount);

        string authority = request.RequestUri!.Authority;
        if (jsonByAuthority.TryGetValue(authority, out string? json))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
