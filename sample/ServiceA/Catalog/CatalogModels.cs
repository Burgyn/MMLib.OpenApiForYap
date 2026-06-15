using System.ComponentModel.DataAnnotations;

namespace ServiceA.Catalog;

/// <summary>Currency a product price is expressed in.</summary>
public enum Currency
{
    Eur,
    Usd,
    Gbp,
}

/// <summary>Availability state of a product.</summary>
public enum ProductAvailability
{
    InStock,
    OutOfStock,
    PreOrder,
    Discontinued,
}

/// <summary>A catalog product.</summary>
public sealed record Product(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    Currency Currency,
    string Category,
    ProductAvailability Availability,
    IReadOnlyList<string> Tags,
    double Rating,
    DateTimeOffset CreatedAt);

/// <summary>A product category with the number of products it contains.</summary>
public sealed record Category(string Slug, string Name, int ProductCount);

/// <summary>Request body to create a product.</summary>
public sealed record CreateProductRequest
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; init; }

    [Range(0.01, 1_000_000)]
    public decimal Price { get; init; }

    public Currency Currency { get; init; } = Currency.Eur;

    [Required]
    [StringLength(60, MinimumLength = 2)]
    public string Category { get; init; } = string.Empty;

    [MaxLength(10)]
    public IReadOnlyList<string>? Tags { get; init; }
}

/// <summary>Request body to fully update a product.</summary>
public sealed record UpdateProductRequest
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; init; }

    [Range(0.01, 1_000_000)]
    public decimal Price { get; init; }

    public Currency Currency { get; init; } = Currency.Eur;

    public ProductAvailability Availability { get; init; } = ProductAvailability.InStock;

    [MaxLength(10)]
    public IReadOnlyList<string>? Tags { get; init; }
}

/// <summary>Request body to change only a product's price.</summary>
public sealed record UpdatePriceRequest
{
    [Range(0.01, 1_000_000)]
    public decimal Price { get; init; }

    public Currency Currency { get; init; } = Currency.Eur;
}

/// <summary>Query parameters for listing products.</summary>
public readonly record struct ProductQuery(
    int? Page,
    int? PageSize,
    string? Search,
    string? Category,
    ProductAvailability? Availability,
    string? Sort);
