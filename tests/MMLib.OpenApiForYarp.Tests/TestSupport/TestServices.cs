using Microsoft.Extensions.DependencyInjection;

namespace MMLib.OpenApiForYarp.Tests;

internal static class TestServices
{
    public static IServiceProvider Empty { get; } = new ServiceCollection().BuildServiceProvider();
}
