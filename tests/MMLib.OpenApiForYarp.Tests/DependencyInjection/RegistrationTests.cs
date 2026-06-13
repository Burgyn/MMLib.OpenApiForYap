using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using MMLib.OpenApiForYarp.Abstractions;
using MMLib.OpenApiForYarp.Pipeline;
using MMLib.OpenApiForYarp.Transformers;
using Yarp.ReverseProxy.Transforms.Builder;

namespace MMLib.OpenApiForYarp.Tests.DependencyInjection;

public class RegistrationTests
{
    private sealed class CustomDoc : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument d, OpenApiDocumentTransformerContext c, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class DualFactory : ITransformFactory, IOpenApiDocumentTransformer
    {
        public bool Validate(TransformRouteValidationContext context, IReadOnlyDictionary<string, string> transformValues) => false;

        public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues) => false;

        public Task TransformAsync(OpenApiDocument d, OpenApiDocumentTransformerContext c, CancellationToken ct) => Task.CompletedTask;
    }

    private static ServiceProvider BuildProvider(Action<IOpenApiForYarpBuilder>? configure = null)
    {
        var services = new ServiceCollection();
        var builder = services.AddReverseProxy().AddOpenApiForYarp();
        configure?.Invoke(builder);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Builtins_Registered_First_In_Order()
    {
        var registry = BuildProvider().GetRequiredService<OpenApiTransformerRegistry>();

        registry.DocumentTransformers.ShouldBe(
        [
            typeof(PathRewriteTransformer),
            typeof(SecurityPropagationTransformer),
            typeof(PublishedPathsFilterTransformer),
        ]);
    }

    [Fact]
    public void CustomTransformer_Appended_After_Builtins()
    {
        var registry = BuildProvider(b => b.AddDocumentTransformer<CustomDoc>())
            .GetRequiredService<OpenApiTransformerRegistry>();

        registry.DocumentTransformers[^1].ShouldBe(typeof(CustomDoc));
        registry.DocumentTransformers.Count.ShouldBe(4);
    }

    [Fact]
    public void ClearOpenApiTransformers_Removes_Builtins()
    {
        var registry = BuildProvider(b => b.ClearOpenApiTransformers().AddDocumentTransformer<CustomDoc>())
            .GetRequiredService<OpenApiTransformerRegistry>();

        registry.DocumentTransformers.ShouldBe([typeof(CustomDoc)]);
    }

    [Fact]
    public void AddTransformFactory_Wires_Both_Interfaces_SameInstance()
    {
        var provider = BuildProvider(b => b.AddTransformFactory<DualFactory>());

        var registry = provider.GetRequiredService<OpenApiTransformerRegistry>();
        registry.DocumentTransformers.ShouldContain(typeof(DualFactory));

        var asFactory = provider.GetServices<ITransformFactory>().OfType<DualFactory>().Single();
        asFactory.ShouldBeSameAs(provider.GetRequiredService<DualFactory>());
    }

    [Fact]
    public void AddTransformFactory_Throws_When_Neither_Interface_Implemented()
    {
        Should.Throw<InvalidOperationException>(() => BuildProvider(b => b.AddTransformFactory<RegistrationTests>()));
    }
}
