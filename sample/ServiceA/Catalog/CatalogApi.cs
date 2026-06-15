using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Sample.Common;

namespace ServiceA.Catalog;

/// <summary>In-memory catalog backing store, seeded with sample products.</summary>
internal sealed class CatalogStore
{
    private readonly ConcurrentDictionary<Guid, Product> _products = new();

    public CatalogStore()
    {
        foreach (Product product in Seed())
        {
            _products[product.Id] = product;
        }
    }

    public IEnumerable<Product> All => _products.Values;

    public Product? Get(Guid id) => _products.TryGetValue(id, out Product? p) ? p : null;

    public Product Add(Product product)
    {
        _products[product.Id] = product;
        return product;
    }

    public bool TryReplace(Guid id, Func<Product, Product> update, out Product? updated)
    {
        if (_products.TryGetValue(id, out Product? existing))
        {
            updated = update(existing);
            _products[id] = updated;
            return true;
        }

        updated = null;
        return false;
    }

    public bool Remove(Guid id) => _products.TryRemove(id, out _);

    private static IEnumerable<Product> Seed()
    {
        var now = DateTimeOffset.UtcNow;
        yield return new Product(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Mechanical Keyboard", "Hot-swappable RGB mechanical keyboard.", 89.90m, Currency.Eur, "peripherals", ProductAvailability.InStock, ["input", "rgb"], 4.6, now.AddDays(-40));
        yield return new Product(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Wireless Mouse", "Ergonomic wireless mouse, 8000 DPI.", 39.50m, Currency.Eur, "peripherals", ProductAvailability.InStock, ["input"], 4.3, now.AddDays(-30));
        yield return new Product(Guid.Parse("33333333-3333-3333-3333-333333333333"), "27\" 4K Monitor", "27-inch 4K IPS display with USB-C.", 379.00m, Currency.Eur, "displays", ProductAvailability.PreOrder, ["screen", "4k"], 4.8, now.AddDays(-12));
        yield return new Product(Guid.Parse("44444444-4444-4444-4444-444444444444"), "USB-C Hub", "7-in-1 USB-C hub with HDMI and PD.", 49.00m, Currency.Eur, "accessories", ProductAvailability.OutOfStock, ["usb-c"], 4.1, now.AddDays(-7));
    }
}

/// <summary>Maps the product and category endpoints.</summary>
internal static class CatalogApi
{
    public static IEndpointRouteBuilder MapCatalog(this IEndpointRouteBuilder app)
    {
        var store = new CatalogStore();

        RouteGroupBuilder products = app.MapGroup("/products").WithTags("Products");

        products.MapGet("/", ([AsParameters] ProductQuery query) =>
            {
                IEnumerable<Product> result = store.All;

                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    result = result.Where(p => p.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(query.Category))
                {
                    result = result.Where(p => string.Equals(p.Category, query.Category, StringComparison.OrdinalIgnoreCase));
                }

                if (query.Availability is { } availability)
                {
                    result = result.Where(p => p.Availability == availability);
                }

                result = query.Sort?.ToLowerInvariant() switch
                {
                    "price" => result.OrderBy(p => p.Price),
                    "-price" => result.OrderByDescending(p => p.Price),
                    "rating" => result.OrderByDescending(p => p.Rating),
                    _ => result.OrderBy(p => p.Name),
                };

                return TypedResults.Ok(result.ToPagedResult(query.Page, query.PageSize));
            })
            .WithName("ListProducts")
            .WithSummary("List products")
            .WithDescription("Returns a paged list of products with optional search, category, availability filters and sorting.");

        products.MapGet("/{id:guid}", Results<Ok<Product>, NotFound<ProblemDetails>> (Guid id) =>
                store.Get(id) is { } product
                    ? TypedResults.Ok(product)
                    : TypedResults.NotFound(Problems.NotFound("Product", id)))
            .WithName("GetProduct")
            .WithSummary("Get a product by id");

        products.MapPost("/", Results<Created<Product>, ValidationProblem> (CreateProductRequest request) =>
            {
                if (!Validation.TryValidate(request, out var errors))
                {
                    return TypedResults.ValidationProblem(errors);
                }

                var product = new Product(
                    Guid.NewGuid(), request.Name, request.Description, request.Price, request.Currency,
                    request.Category, ProductAvailability.InStock, request.Tags ?? [], 0, DateTimeOffset.UtcNow);
                store.Add(product);
                return TypedResults.Created($"/products/{product.Id}", product);
            })
            .WithName("CreateProduct")
            .WithSummary("Create a product");

        products.MapPut("/{id:guid}", Results<Ok<Product>, NotFound<ProblemDetails>, ValidationProblem> (Guid id, UpdateProductRequest request) =>
            {
                if (!Validation.TryValidate(request, out var errors))
                {
                    return TypedResults.ValidationProblem(errors);
                }

                return store.TryReplace(id, existing => existing with
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Currency = request.Currency,
                    Availability = request.Availability,
                    Tags = request.Tags ?? existing.Tags,
                }, out Product? updated)
                    ? TypedResults.Ok(updated!)
                    : TypedResults.NotFound(Problems.NotFound("Product", id));
            })
            .WithName("UpdateProduct")
            .WithSummary("Replace a product");

        products.MapPatch("/{id:guid}/price", Results<Ok<Product>, NotFound<ProblemDetails>, ValidationProblem> (Guid id, UpdatePriceRequest request) =>
            {
                if (!Validation.TryValidate(request, out var errors))
                {
                    return TypedResults.ValidationProblem(errors);
                }

                return store.TryReplace(id, existing => existing with { Price = request.Price, Currency = request.Currency }, out Product? updated)
                    ? TypedResults.Ok(updated!)
                    : TypedResults.NotFound(Problems.NotFound("Product", id));
            })
            .WithName("UpdateProductPrice")
            .WithSummary("Update a product's price");

        products.MapDelete("/{id:guid}", Results<NoContent, NotFound<ProblemDetails>> (Guid id) =>
                store.Remove(id)
                    ? TypedResults.NoContent()
                    : TypedResults.NotFound(Problems.NotFound("Product", id)))
            .WithName("DeleteProduct")
            .WithSummary("Delete a product");

        RouteGroupBuilder categories = app.MapGroup("/categories").WithTags("Categories");

        categories.MapGet("/", () => TypedResults.Ok(BuildCategories(store)))
            .WithName("ListCategories")
            .WithSummary("List categories");

        categories.MapGet("/{slug}/products", ([AsParameters] ProductQuery query, string slug) =>
                TypedResults.Ok(store.All
                    .Where(p => string.Equals(p.Category, slug, StringComparison.OrdinalIgnoreCase))
                    .ToPagedResult(query.Page, query.PageSize)))
            .WithName("ListCategoryProducts")
            .WithSummary("List products in a category");

        return app;
    }

    private static IReadOnlyList<Category> BuildCategories(CatalogStore store)
        => [.. store.All
            .GroupBy(p => p.Category)
            .Select(g => new Category(g.Key, Capitalize(g.Key), g.Count()))
            .OrderBy(c => c.Slug)];

    private static string Capitalize(string value)
        => string.IsNullOrEmpty(value) ? value : char.ToUpperInvariant(value[0]) + value[1..];
}
