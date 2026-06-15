using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Sample.Common;

/// <summary>
/// Shared OpenAPI configuration for the sample downstream services: production-looking document
/// metadata, security schemes, and a per-operation security requirement on write operations.
/// </summary>
public static class SampleOpenApiExtensions
{
    /// <summary>
    /// Registers an OpenAPI document with rich <c>info</c>, a <c>Bearer</c> security scheme (and an
    /// optional <c>ApiKey</c> scheme), and marks every write operation (POST/PUT/PATCH/DELETE) as
    /// requiring Bearer auth.
    /// </summary>
    public static IServiceCollection AddSampleOpenApi(
        this IServiceCollection services,
        string title,
        string description,
        bool includeApiKey = false)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info.Title = title;
                document.Info.Version = "1.0.0";
                document.Info.Description = description;
                document.Info.Contact = new OpenApiContact
                {
                    Name = "MMLib.OpenApiForYarp sample",
                    Url = new Uri("https://github.com/Burgyn/MMLib.OpenApiForYarp"),
                };
                document.Info.License = new OpenApiLicense { Name = "MIT" };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT bearer token issued by the identity provider.",
                };
                if (includeApiKey)
                {
                    document.Components.SecuritySchemes["ApiKey"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.ApiKey,
                        Name = "X-Api-Key",
                        In = ParameterLocation.Header,
                        Description = "Service-to-service API key.",
                    };
                }

                ApplyWriteOperationSecurity(document);
                return Task.CompletedTask;
            });
        });

        return services;
    }

    private static void ApplyWriteOperationSecurity(OpenApiDocument document)
    {
        if (document.Paths is null)
        {
            return;
        }

        var bearer = new OpenApiSecuritySchemeReference("Bearer", document);

        foreach (KeyValuePair<string, IOpenApiPathItem> path in document.Paths)
        {
            if (path.Value is not OpenApiPathItem { Operations: { } operations })
            {
                continue;
            }

            foreach (KeyValuePair<HttpMethod, OpenApiOperation> entry in operations)
            {
                if (entry.Key != HttpMethod.Get && entry.Key != HttpMethod.Head && entry.Key != HttpMethod.Options)
                {
                    entry.Value.Security =
                    [
                        new OpenApiSecurityRequirement { [bearer] = new List<string>() },
                    ];
                }
            }
        }
    }
}
