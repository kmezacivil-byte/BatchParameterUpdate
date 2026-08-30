namespace BatchParameterUpdate.Core;

/// <summary>
/// The result of attempting to update one element.
///
/// ElementId is stored as a plain <see cref="long"/> rather than
/// Autodesk.Revit.DB.ElementId on purpose: it is the one field that would
/// otherwise force this project to reference the Revit API, and a long is all
/// a summary or a log line needs. The Revit-side code converts
/// ElementId.Value (Revit 2024+) or ElementId.IntegerValue (2023 and earlier)
/// to long when building this record.
/// </summary>
public sealed record ElementOutcome(
    long ElementId,
    string ElementDescription,
    UpdateOutcome Outcome,
    SkipReason? Reason,
    string Detail)
{
    public static ElementOutcome Updated(long elementId, string elementDescription, string detail) =>
        new(elementId, elementDescription, UpdateOutcome.Updated, Reason: null, detail);

    public static ElementOutcome Unchanged(long elementId, string elementDescription, string detail) =>
        new(elementId, elementDescription, UpdateOutcome.Unchanged, Reason: null, detail);

    public static ElementOutcome Skipped(long elementId, string elementDescription, SkipReason reason, string detail) =>
        new(elementId, elementDescription, UpdateOutcome.Skipped, reason, detail);
}
