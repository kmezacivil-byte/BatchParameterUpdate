using System.Windows;
using System.Windows.Interop;
using BatchParameterUpdate.Core;

namespace BatchParameterUpdate.Revit.UI;

/// <summary>
/// Code-behind is intentionally thin: it wires the ViewModel, sets the native
/// window owner so the dialog stays modal to Revit's main window, and closes
/// itself when the ViewModel reports a result. No parameter logic lives here.
/// </summary>
public partial class ParameterInputDialog : Window
{
    private readonly ParameterInputViewModel _viewModel = new();

    public ParameterInputDialog(IntPtr revitMainWindowHandle)
    {
        InitializeComponent();

        // Without this, the dialog opens behind the Revit main window instead
        // of in front of it - WPF windows are not Revit-aware by default.
        new WindowInteropHelper(this).Owner = revitMainWindowHandle;

        DataContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Loaded += (_, _) => ParameterNameTextBox.Focus();
    }

    /// <summary>Null when the user cancelled or closed the dialog without
    /// confirming; a validated request otherwise.</summary>
    public ParameterUpdateRequest? Result { get; private set; }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ParameterInputViewModel.DialogResult))
            return;

        if (_viewModel.DialogResult == true)
            Result = _viewModel.CurrentRequest;

        // Setting DialogResult on a window shown via ShowDialog() already
        // closes it - an explicit Close() call here would be redundant.
        DialogResult = _viewModel.DialogResult;
    }
}
