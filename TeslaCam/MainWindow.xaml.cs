using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using TeslaCam.Data;
using Wpf.Ui.Appearance;

namespace TeslaCam;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[ObservableObject]
public partial class MainWindow : Window
{
    [ObservableProperty]
    private CamClip _currentClip;

    [ObservableProperty]
    private string _errorMessage;

    public ObservableCollection<CamClip> Clips { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
        ApplicationThemeManager.Apply(this);
        DataContext = this;

        var _storages = new HashSet<CamStorage>();

        // Local directory.
        if (Directory.Exists("./TeslaCam"))
        {
            _storages.Add(new CamStorage("./TeslaCam"));
        }

        // USB sticks.
        foreach (var storage in CamStorage.GetSticks())
        {
            _storages.Add(storage);
        }

        if (_storages.Count == 0)
        {
            Log.Debug("No storages found");
            ErrorMessage = "No TeslaCam folders found";
        }
        else
        {
            Log.Debug($"Found storages: {string.Join(", ", _storages)}");

            foreach (var clips in _storages.SelectMany(x => x.Clips))
            {
                Clips.Add(clips);
            }
        }

        CurrentClip = Clips.FirstOrDefault();
    }
}
