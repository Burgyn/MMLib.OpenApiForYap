namespace MMLib.OpenApiForYarp.PathTransformation;

/// <summary>
/// Helpers for reasoning about ASP.NET Core route templates used by YARP route matches and
/// path transforms (e.g. <c>/api/products/{**catch-all}</c>, <c>/products/{id}</c>).
/// </summary>
internal static class YarpPathTemplate
{
    /// <summary>
    /// Returns the literal (non-parameterized) leading portion of a route template: the segments
    /// before the first <c>{...}</c> token. For <c>/api/products/{**catch-all}</c> this is
    /// <c>/api/products</c>; for <c>{**catch-all}</c> it is the empty string.
    /// </summary>
    public static string StaticPrefix(string? template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        bool leadingSlash = template[0] == '/';
        string body = leadingSlash ? template[1..] : template;

        List<string> taken = [];
        foreach (string segment in body.Split('/'))
        {
            if (segment.Contains('{', StringComparison.Ordinal))
            {
                break;
            }

            taken.Add(segment);
        }

        string prefix = string.Join('/', taken);
        if (leadingSlash)
        {
            prefix = "/" + prefix;
        }

        // Normalize a bare root and trailing slashes ("/", "/app/v1/" -> "" / "/app/v1").
        prefix = prefix.TrimEnd('/');
        return prefix;
    }

    /// <summary>
    /// Joins a static prefix with a path remainder using exactly one separating slash and a single
    /// leading slash. Either argument may be empty or already slash-prefixed.
    /// </summary>
    public static string Combine(string prefix, string remainder)
    {
        string left = (prefix ?? string.Empty).TrimEnd('/');
        string right = (remainder ?? string.Empty).Trim('/');

        if (left.Length == 0)
        {
            return right.Length == 0 ? "/" : "/" + right;
        }

        return right.Length == 0 ? left : left + "/" + right;
    }
}
