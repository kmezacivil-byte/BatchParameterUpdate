namespace BatchParameterUpdate.Core;

/// <summary>
/// User-supplied input: which parameter to write, and what value to write
/// into it. Validation lives here (not in the WPF ViewModel) so the exact same
/// rule is unit-tested and reused if a second entry point is ever added.
/// </summary>
public sealed record ParameterUpdateRequest(string ParameterName, string NewValue)
{
    /// <summary>
    /// True when ParameterName is non-empty after trimming. NewValue is
    /// intentionally NOT required to be non-empty: clearing a parameter to an
    /// empty string is a legitimate, common use case.
    /// </summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(ParameterName);

    /// <summary>Parameter name with leading/trailing whitespace removed, used
    /// for both matching and display.</summary>
    public string TrimmedParameterName => ParameterName.Trim();
}
