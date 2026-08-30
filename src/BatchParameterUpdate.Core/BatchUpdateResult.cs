namespace BatchParameterUpdate.Core;

/// <summary>
/// Aggregates the per-element outcomes of one run into counts and a
/// human-readable summary. Pure computation over already-known outcomes, so it
/// is fully unit-testable without a Revit session.
/// </summary>
public sealed class BatchUpdateResult
{
    public IReadOnlyList<ElementOutcome> Outcomes { get; }

    public BatchUpdateResult(IReadOnlyList<ElementOutcome> outcomes)
    {
        Outcomes = outcomes;
    }

    public int TotalCount => Outcomes.Count;
    public int UpdatedCount => Outcomes.Count(o => o.Outcome == UpdateOutcome.Updated);
    public int UnchangedCount => Outcomes.Count(o => o.Outcome == UpdateOutcome.Unchanged);
    public int SkippedCount => Outcomes.Count(o => o.Outcome == UpdateOutcome.Skipped);

    /// <summary>Skip counts broken down by reason, ordered by frequency
    /// (most common first) so the summary reads like a diagnosis, not a dump.</summary>
    public IReadOnlyList<(SkipReason Reason, int Count)> SkippedByReason =>
        Outcomes
            .Where(o => o is { Outcome: UpdateOutcome.Skipped, Reason: not null })
            .GroupBy(o => o.Reason!.Value)
            .Select(g => (Reason: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

    /// <summary>
    /// Builds the text shown in the result TaskDialog: a one-line total plus a
    /// breakdown of skip reasons. Kept here (not in the Revit command) so the
    /// exact wording is covered by unit tests.
    /// </summary>
    public string BuildSummary()
    {
        if (TotalCount == 0)
            return "No elements were processed.";

        var lines = new List<string>
        {
            $"{UpdatedCount} updated, {UnchangedCount} unchanged, {SkippedCount} skipped (of {TotalCount} selected)."
        };

        if (SkippedByReason.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("Skipped elements:");
            lines.AddRange(SkippedByReason.Select(x => $"  - {Describe(x.Reason)}: {x.Count}"));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string Describe(SkipReason reason) => reason switch
    {
        SkipReason.NotAModelElement => "not a model element instance",
        SkipReason.ParameterNotFound => "parameter not found",
        SkipReason.TypeParameterOnly => "parameter exists on the type only",
        SkipReason.AmbiguousParameterName => "parameter name is ambiguous",
        SkipReason.NotATextParameter => "parameter is not a text parameter",
        SkipReason.ReadOnlyParameter => "parameter is read-only",
        SkipReason.SetRejectedByRevit => "Revit rejected the write",
        SkipReason.ElementNotOwned => "element is not owned in the current workset",
        _ => reason.ToString()
    };
}
