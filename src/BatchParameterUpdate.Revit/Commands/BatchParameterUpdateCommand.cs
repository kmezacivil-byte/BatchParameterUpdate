using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BatchParameterUpdate.Revit.Services;
using BatchParameterUpdate.Revit.UI;

namespace BatchParameterUpdate.Revit.Commands;

/// <summary>
/// Entry point of the batch update. Stays thin on purpose: it validates
/// preconditions, collects input via the dialog, and delegates the actual
/// work to BatchParameterUpdateService. No parameter logic lives here.
/// </summary>
[Transaction(TransactionMode.Manual)]
public sealed class BatchParameterUpdateCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiApplication = commandData.Application;
        var uiDocument = uiApplication.ActiveUIDocument;

        if (uiDocument?.Document is null)
        {
            message = "There is no active Revit document.";
            return Result.Failed;
        }

        var document = uiDocument.Document;

        // The selection is captured as the very first action, before any
        // dialog is shown, so the operation acts exactly on what was
        // selected when the command started.
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

        var dialog = new ParameterInputDialog(uiApplication.MainWindowHandle);
        var dialogAccepted = dialog.ShowDialog();

        // dialogAccepted is null if the window was closed without setting
        // DialogResult at all (e.g. Alt+F4); Result stays null in that case
        // too, so both paths are covered by this one check.
        if (dialogAccepted != true || dialog.Result is not { } request)
            return Result.Cancelled;

        var result = BatchParameterUpdateService.Run(document, selectedIds, request);

        TaskDialog.Show("Batch Parameter Update", result.BuildSummary());

        return Result.Succeeded;
    }
}
