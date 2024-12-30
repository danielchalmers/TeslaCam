using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
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
    private CamClip _currentClip;

    [ObservableProperty]
    private string _errorMessage;

    public ObservableCollection<CamClip> Clips { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
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
            ErrorMessage = "No TeslaCam folders found. Insert the USB stick and restart.";
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

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Debug($"User is selecting a folder");

        var dialog = new OpenFolderDialog();
        dialog.Title = "Select a folder containing dashcam footage";

        var result = dialog.ShowDialog();

        if (result != true)
        {
            Log.Debug($"No folder was selected");
            return;
        }

        CurrentClip = null;
        Clips.Clear();

        CamStorage storage;
        try
        {
            storage = new CamStorage(dialog.FolderName);
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Debug(ex, "Access denied");
            ErrorMessage = "Access denied. Please run the application as an administrator.";
            return;
        }

        Log.Debug($"Loading clips from {dialog.FolderName}");

        foreach (var clip in storage.Clips)
        {
            Clips.Add(clip);
        }

        CurrentClip = Clips.FirstOrDefault();
    }
}
