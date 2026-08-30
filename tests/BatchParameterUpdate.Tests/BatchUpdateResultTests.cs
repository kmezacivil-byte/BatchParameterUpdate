using BatchParameterUpdate.Core;
using Xunit;

namespace BatchParameterUpdate.Tests;

public class BatchUpdateResultTests
{
    [Fact]
    public void Counts_are_computed_per_outcome_kind()
    {
        var outcomes = new[]
        {
            ElementOutcome.Updated(1, "Wall 1", "Set to 'A'"),
            ElementOutcome.Updated(2, "Wall 2", "Set to 'A'"),
            ElementOutcome.Unchanged(3, "Wall 3", "Already 'A'"),
            ElementOutcome.Skipped(4, "Wall 4", SkipReason.ParameterNotFound, "not found"),
            ElementOutcome.Skipped(5, "Wall 5", SkipReason.ParameterNotFound, "not found"),
            ElementOutcome.Skipped(6, "Wall 6", SkipReason.ReadOnlyParameter, "read only"),
        };

        var result = new BatchUpdateResult(outcomes);

        Assert.Equal(6, result.TotalCount);
        Assert.Equal(2, result.UpdatedCount);
        Assert.Equal(1, result.UnchangedCount);
        Assert.Equal(3, result.SkippedCount);
    }

    [Fact]
    public void SkippedByReason_groups_and_orders_by_frequency_descending()
    {
        var outcomes = new[]
        {
            ElementOutcome.Skipped(1, "A", SkipReason.ReadOnlyParameter, "x"),
            ElementOutcome.Skipped(2, "B", SkipReason.ParameterNotFound, "x"),
            ElementOutcome.Skipped(3, "C", SkipReason.ParameterNotFound, "x"),
            ElementOutcome.Skipped(4, "D", SkipReason.ParameterNotFound, "x"),
        };

        var result = new BatchUpdateResult(outcomes);
        var breakdown = result.SkippedByReason;

        Assert.Equal(SkipReason.ParameterNotFound, breakdown[0].Reason);
        Assert.Equal(3, breakdown[0].Count);
        Assert.Equal(SkipReason.ReadOnlyParameter, breakdown[1].Reason);
        Assert.Equal(1, breakdown[1].Count);
    }

    [Fact]
    public void BuildSummary_reports_zero_elements_when_nothing_was_processed()
    {
        var result = new BatchUpdateResult(Array.Empty<ElementOutcome>());

        Assert.Equal("No elements were processed.", result.BuildSummary());
    }

    [Fact]
    public void BuildSummary_includes_totals_and_skip_breakdown()
    {
        var outcomes = new[]
        {
            ElementOutcome.Updated(1, "A", "x"),
            ElementOutcome.Skipped(2, "B", SkipReason.ReadOnlyParameter, "x"),
        };

        var summary = new BatchUpdateResult(outcomes).BuildSummary();

        Assert.Contains("1 updated, 0 unchanged, 1 skipped (of 2 selected).", summary);
        Assert.Contains("read-only", summary);
    }
}
