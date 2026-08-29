using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BatchParameterUpdate.Revit.Commands;

/// <summary>
/// Entry point of the batch update. This class is intentionally thin: it
/// validates preconditions, collects user input and delegates the actual work.
/// No parameter logic lives here.
/// </summary>
[Transaction(TransactionMode.Manual)]
public sealed class BatchParameterUpdateCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDocument = commandData.Application.ActiveUIDocument;

        if (uiDocument?.Document is null)
        {
            message = "There is no active Revit document.";
            return Result.Failed;
        }

        var document = uiDocument.Document;

        // The selection is captured as the very first action, before any dialog
        // is shown, so the operation acts exactly on what was selected when the
        // command started.
        var selectedIds = uiDocument.Selection.GetElementIds().ToList();

        if (document.IsFamilyDocument)
        {
            TaskDialog.Show("Batch Parameter Update",
                "This command runs on project documents only. The active document is a family document.");
            return Result.Cancelled;
        }

        if (document.IsReadOnly)
        {
            TaskDialog.Show("Batch Parameter Update",
                "The active document is read-only and cannot be modified.");
            return Result.Cancelled;
        }

        if (selectedIds.Count == 0)
        {
            TaskDialog.Show("Batch Parameter Update",
                "No elements are selected.\n\nSelect one or more model elements in the model, then run the command again.");
            return Result.Cancelled;
        }

        // --- Scaffold milestone ---------------------------------------------
        // Next commits: input dialog (UI), parameter resolution (Core),
        // transactional update and result summary.
        TaskDialog.Show("Batch Parameter Update",
            $"Add-in loaded successfully.\n\nElements captured in the selection: {selectedIds.Count}");

        return Result.Succeeded;
    }
}
