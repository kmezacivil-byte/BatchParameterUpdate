using Autodesk.Revit.DB;
using BatchParameterUpdate.Core;
using BatchParameterUpdate.Revit.Resolution;

namespace BatchParameterUpdate.Revit.Services;

/// <summary>
/// Orchestrates one batch run: opens a single transaction, resolves every
/// selected element against the request, and aggregates the outcomes.
///
/// Per-element failures (a missing parameter, a read-only parameter, an
/// ambiguous name) are pre-validated inside ParameterResolver and never throw
/// - they come back as a Skipped outcome, so they do not affect the
/// transaction. Only a genuine, unexpected exception rolls back the entire
/// batch, which is what keeps the "all or nothing" guarantee: a bug must not
/// leave the model partially modified.
/// </summary>
public static class BatchParameterUpdateService
{
    public static BatchUpdateResult Run(Document document, IReadOnlyCollection<ElementId> selectedIds, ParameterUpdateRequest request)
    {
        var outcomes = new List<ElementOutcome>(selectedIds.Count);

        using var transaction = new Transaction(document, "Batch Parameter Update");
        transaction.Start();

        try
        {
            foreach (var id in selectedIds)
            {
                // GetElement can return null if an id refers to something
                // already deleted between selection and execution (e.g. by
                // another add-in or a linked-model refresh). Rather than
                // crash the whole batch over one stale id, it is treated as
                // a normal skip.
                var element = document.GetElement(id);
                if (element is null)
                {
                    outcomes.Add(ElementOutcome.Skipped(id.Value, $"Unknown element (Id {id.Value})",
                        SkipReason.NotAModelElement, "Element no longer exists in the document."));
                    continue;
                }

                outcomes.Add(ParameterResolver.Resolve(element, request));
            }

            transaction.Commit();
        }
        catch
        {
            if (transaction.GetStatus() == TransactionStatus.Started)
                transaction.RollBack();
            throw;
        }

        return new BatchUpdateResult(outcomes);
    }
}
