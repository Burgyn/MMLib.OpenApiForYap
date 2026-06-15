using System.ComponentModel.DataAnnotations;

namespace Sample.Common;

/// <summary>DataAnnotations validation helper returning a problem-details-friendly error map.</summary>
public static class Validation
{
    /// <summary>Validates <paramref name="instance"/>; on failure returns errors keyed by member name.</summary>
    public static bool TryValidate<T>(T instance, out Dictionary<string, string[]> errors)
        where T : notnull
    {
        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();

        if (Validator.TryValidateObject(instance, context, results, validateAllProperties: true))
        {
            errors = [];
            return true;
        }

        errors = results
            .SelectMany(r => r.MemberNames.DefaultIfEmpty(string.Empty), (r, member) => (Member: member, r.ErrorMessage))
            .GroupBy(x => x.Member, x => x.ErrorMessage ?? "Invalid value.")
            .ToDictionary(g => g.Key, g => g.ToArray());
        return false;
    }
}
