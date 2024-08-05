using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using TeslaCam.Data;

namespace TeslaCam;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[ObservableObject]
public partial class MainWindow : Window
{
    [ObservableProperty]
    private CamStorage _camStorage;

    [ObservableProperty]
    private CamClip _currentClip;

    [ObservableProperty]
    private string _errorMessage;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        _camStorage = CamStorage.GetSticks().FirstOrDefault();
        _camStorage ??= new CamStorage("./TeslaCam"); // Fall back to local directory.
        Log.Debug($"Found storage: {_camStorage}");

        CurrentClip = _camStorage.Clips.FirstOrDefault();
        Log.Debug($"Current clip: {CurrentClip}");
    }
}
