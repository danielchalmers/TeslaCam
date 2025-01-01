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

        var roots = CamStorage.FindCommonRoots().ToList();

        if (roots.Count == 0)
        {
            Log.Debug("No common roots found");
            ErrorMessage = "No TeslaCam folders found";
        }
        else
        {
            foreach (var root in roots)
            {
                Log.Debug($"Found root: {root}");
                var storage = CamStorage.Traverse(root);
                foreach (var clips in storage.Clips)
                {
                    Clips.Add(clips);
                }
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

        Log.Debug($"Using new root: {dialog.FolderName}");

        // The user has committed at this point, even if it doesn't end up loading. Lets clear the current state.
        ErrorMessage = null;
        CurrentClip = null;
        Clips.Clear();

        CamStorage storage;
        try
        {
            storage = CamStorage.Traverse(dialog.FolderName);
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Debug(ex, "Access denied");
            ErrorMessage = "Access to folder denied";
            return;
        }

        foreach (var clip in storage.Clips)
        {
            Clips.Add(clip);
        }

        CurrentClip = Clips.FirstOrDefault();
    }
}
