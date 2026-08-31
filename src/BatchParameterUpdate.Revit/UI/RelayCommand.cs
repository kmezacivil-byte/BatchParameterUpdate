using System.Windows.Input;

namespace BatchParameterUpdate.Revit.UI;

/// <summary>
/// Minimal ICommand implementation for a single dialog with two commands
/// (OK/Cancel). A full MVVM library (CommunityToolkit.Mvvm, Prism, etc.) was
/// deliberately not added as a dependency: it would be one more package for
/// the installer to justify, for a problem that ~15 lines already solve.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public event EventHandler? CanExecuteChanged;

    /// <summary>Call after a bound property changes so WPF re-queries
    /// CanExecute (e.g. to enable/disable the OK button as the user types).</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
