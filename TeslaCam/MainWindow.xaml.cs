using System.Collections.ObjectModel;
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

        var storages = CamStorage.FindStorages();

        if (storages.Count == 0)
        {
            Log.Debug("No storages found");
            ErrorMessage = "No TeslaCam folders found";
        }
        else
        {
            Log.Debug($"Found storages: {string.Join(", ", storages)}");

            foreach (var clips in storages.SelectMany(x => x.Clips))
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

        // The user has committed at this point, even if it doesn't end up loading. Lets clear the current state.
        ErrorMessage = null;
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
            ErrorMessage = "Access to folder denied";
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
