using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BatchParameterUpdate.Core;

namespace BatchParameterUpdate.Revit.UI;

/// <summary>
/// ViewModel for the parameter input dialog. Validation is delegated to
/// Core.ParameterUpdateRequest.IsValid rather than duplicated here: it is the
/// same rule already covered by ParameterUpdateRequestTests, so the dialog
/// and the unit tests can never silently disagree about what counts as valid.
/// </summary>
public sealed class ParameterInputViewModel : INotifyPropertyChanged
{
    private string _parameterName = string.Empty;
    private string _newValue = string.Empty;
    private bool? _dialogResult;

    private readonly RelayCommand _okCommand; 
    public ParameterInputViewModel() 
    { _okCommand = new RelayCommand(Confirm, () => CurrentRequest.IsValid); 
        OkCommand = _okCommand; 
        CancelCommand = new RelayCommand(Cancel); }

    public string ParameterName
    {
        get => _parameterName;
        set
        {
            if (_parameterName == value) return;
            _parameterName = value;
            OnPropertyChanged();
            _okCommand.RaiseCanExecuteChanged();
        }
    }

    public string NewValue
    {
        get => _newValue;
        set
        {
            if (_newValue == value) return;
            _newValue = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Bound to Window.DialogResult via code-behind; true = OK, false = Cancel/closed.</summary>
    public bool? DialogResult
    {
        get => _dialogResult;
        private set
        {
            _dialogResult = value;
            OnPropertyChanged();
        }
    }

    public ICommand OkCommand { get; }
    public ICommand CancelCommand { get; }

    /// <summary>The validated request, built fresh from the current textbox
    /// values every time it's read. Read this after DialogResult == true.</summary>
    public ParameterUpdateRequest CurrentRequest => new(ParameterName, NewValue);

    private void Confirm() => DialogResult = true;

    private void Cancel() => DialogResult = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
