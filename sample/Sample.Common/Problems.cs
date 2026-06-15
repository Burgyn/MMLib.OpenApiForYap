using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Sample.Common;

/// <summary>Factory for RFC 7807 problem responses used across the sample services.</summary>
public static class Problems
{
    /// <summary>Builds a 404 problem for a missing resource.</summary>
    public static ProblemDetails NotFound(string resource, object id) => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Resource not found",
        Detail = $"{resource} '{id}' was not found.",
        Type = "https://httpstatuses.io/404",
    };

    /// <summary>Builds a 409 conflict problem.</summary>
    public static ProblemDetails Conflict(string detail) => new()
    {
        Status = StatusCodes.Status409Conflict,
        Title = "Conflict",
        Detail = detail,
        Type = "https://httpstatuses.io/409",
    };
}
