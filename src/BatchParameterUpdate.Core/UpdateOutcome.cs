namespace BatchParameterUpdate.Core;

/// <summary>
/// What happened to a single element. "Unchanged" is kept separate from
/// "Updated" on purpose: writing the same value back is not a failure, but
/// reporting it as "updated" would overstate how many elements actually
/// changed.
/// </summary>
public enum UpdateOutcome
{
    Updated,
    Unchanged,
    Skipped
}
