namespace Sample.Common;

/// <summary>
/// A monetary amount in a given currency. A deliberately <em>shared</em> value object: it is exposed
/// by more than one downstream service, so it appears as the same <c>Money</c> schema in several
/// per-service documents and is de-duplicated into a single component in the merged document.
/// </summary>
/// <param name="Amount">The amount.</param>
/// <param name="Currency">ISO-4217 currency code (e.g. <c>EUR</c>).</param>
public sealed record Money(decimal Amount, string Currency);
