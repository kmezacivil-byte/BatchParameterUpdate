using System.Reflection;
using Autodesk.Revit.UI;

namespace BatchParameterUpdate.Revit;

/// <summary>
/// Add-in entry point. Its only responsibility is to expose the command in the
/// Revit ribbon; it holds no state and performs no model work.
/// </summary>
public sealed class App : IExternalApplication
{
    private const string PanelName = "Batch Parameter Update";

    public Result OnStartup(UIControlledApplication application)
    {
        // The panel is created on the built-in "Add-Ins" tab rather than in a
        // dedicated custom tab: a single-command add-in does not justify
        // occupying a top-level tab in the user's ribbon.
        var panel = application.CreateRibbonPanel(PanelName);

        var assemblyPath = Assembly.GetExecutingAssembly().Location;

        var buttonData = new PushButtonData(
            name: "BatchParameterUpdateButton",
            text: "Batch\nParameter",
            assemblyName: assemblyPath,
            className: typeof(Commands.BatchParameterUpdateCommand).FullName)
        {
            ToolTip = "Updates a text instance parameter on the selected elements.",
            LongDescription =
                "Select one or more model elements, then run the command. " +
                "Enter a parameter name and a new value; every selected element " +
                "whose writable text instance parameter matches will be updated. " +
                "Elements that cannot be updated are skipped and reported."
        };

        // No IExternalCommandAvailability is attached on purpose. Disabling the
        // button on an empty selection would hide the empty-selection feedback
        // path, which the assessment explicitly requires the add-in to handle.
        panel.AddItem(buttonData);

        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;
}
