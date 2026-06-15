namespace Sample.Common;

/// <summary>A page of results plus paging metadata. Returned by list endpoints.</summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>The total number of pages for the current page size.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Whether a next page exists.</summary>
    public bool HasNext => Page < TotalPages;
}

/// <summary>Helpers for paginating in-memory collections.</summary>
public static class Paging
{
    /// <summary>The default page size when none/invalid is supplied.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>The maximum allowed page size.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Paginates a sequence, clamping <paramref name="page"/> and <paramref name="pageSize"/> to sane bounds.</summary>
    public static PagedResult<T> ToPagedResult<T>(this IEnumerable<T> source, int? page, int? pageSize)
    {
        int resolvedPage = page is null or < 1 ? 1 : page.Value;
        int resolvedSize = pageSize is null or < 1 ? DefaultPageSize : Math.Min(pageSize.Value, MaxPageSize);

        IReadOnlyList<T> all = source as IReadOnlyList<T> ?? source.ToList();
        List<T> items = [.. all.Skip((resolvedPage - 1) * resolvedSize).Take(resolvedSize)];

        return new PagedResult<T>(items, resolvedPage, resolvedSize, all.Count);
    }
}
