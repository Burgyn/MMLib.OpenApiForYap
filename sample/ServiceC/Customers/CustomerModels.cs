using System.ComponentModel.DataAnnotations;

namespace ServiceC.Customers;

/// <summary>Loyalty tier a customer belongs to.</summary>
public enum LoyaltyTier
{
    Bronze,
    Silver,
    Gold,
    Platinum,
}

/// <summary>A customer profile.</summary>
public sealed record Customer(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    LoyaltyTier Tier,
    IReadOnlyList<Address> Addresses,
    DateTimeOffset CreatedAt);

/// <summary>A postal address belonging to a customer.</summary>
public sealed record Address(
    Guid Id,
    string Line1,
    string? Line2,
    string City,
    string PostalCode,
    string Country,
    bool IsPrimary);

/// <summary>Request body to create a customer.</summary>
public sealed record CreateCustomerRequest
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Phone]
    public string? Phone { get; init; }

    public LoyaltyTier Tier { get; init; } = LoyaltyTier.Bronze;
}

/// <summary>Request body to fully update a customer.</summary>
public sealed record UpdateCustomerRequest
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Phone]
    public string? Phone { get; init; }

    public LoyaltyTier Tier { get; init; }
}

/// <summary>Request body to add an address to a customer.</summary>
public sealed record CreateAddressRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Line1 { get; init; } = string.Empty;

    public string? Line2 { get; init; }

    [Required]
    public string City { get; init; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PostalCode { get; init; } = string.Empty;

    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string Country { get; init; } = string.Empty;

    public bool IsPrimary { get; init; }
}

/// <summary>Query parameters for listing customers.</summary>
public readonly record struct CustomerQuery(
    int? Page,
    int? PageSize,
    string? Search,
    LoyaltyTier? Tier,
    string? Sort);
