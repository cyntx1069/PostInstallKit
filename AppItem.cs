using System.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace PostInstallKit;

public class AppItem : INotifyPropertyChanged
{
    private static readonly Brush GrayBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 74, 90, 110));
    private static readonly Brush GreenBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 52, 211, 153));
    private static readonly Brush RedBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 248, 113, 113));
    private static readonly Brush CyanBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 34, 211, 238));

    private bool _isSelected;
    private bool _isInstalled;
    private string _status = "";

    public string Name { get; set; } = "";
    public string WingetId { get; set; } = "";
    public string Category { get; set; } = "";

    // Optional keyword to look for in Windows' installed programs list.
    // If empty, Name is used.
    public string Detect { get; set; } = "";

    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected != value) { _isSelected = value; Notify(nameof(IsSelected)); } }
    }

    public bool IsInstalled
    {
        get => _isInstalled;
        set { if (_isInstalled != value) { _isInstalled = value; Notify(nameof(IsInstalled)); } }
    }

    public string Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            _status = value;
            Notify(nameof(Status));
            Notify(nameof(StatusBrush));
        }
    }

    public Brush StatusBrush => _status switch
    {
        "Installed" => GreenBrush,
        "Failed" => RedBrush,
        "Installing..." => CyanBrush,
        _ => GrayBrush,
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}