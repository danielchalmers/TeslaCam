using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using Serilog;
using TeslaCam.Data;
using TeslaCam.Processor;

namespace TeslaCam;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[ObservableObject]
public partial class MainWindow : Window
{
    private readonly FFmpegHandler _ffmpeg;
    private readonly List<CamClip> _clips = [];

    [ObservableProperty]
    private CamClip _currentClip;

    [ObservableProperty]
    private string _currentVideoFile;

    [ObservableProperty]
    private string _errorMessage;

    [ObservableProperty]
    private string _filterText = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        var roots = CamStorage.FindCommonRoots().ToList();

        if (roots.Count == 0)
        {
            Log.Information("No common root folders found");
            ErrorMessage = "No TeslaCam folders found";
        }
        else
        {
            foreach (var root in roots)
            {
                Log.Information($"Found root folder: {root}");
                var storage = CamStorage.Map(root);
                _clips.AddRange(storage.Clips);
            }
        }

        _ffmpeg = new("ffmpeg");

        PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(CurrentClip))
            {
                await _ffmpeg.StartNewClip(CurrentClip);
                CurrentVideoFile = await _ffmpeg.CreateVideoForNextChunk();
            }
        };
    }

    /// <summary>
    /// A proxy for the clips list that handles ordering and filtering.
    /// </summary>
    public IReadOnlyList<CamClip> Clips => _clips
        .Where(x => x.Summary.Contains(FilterText, StringComparison.CurrentCultureIgnoreCase))
        .OrderByDescending(x => x.Timestamp) // Order newest by timestamp, either from folder name or event data.
        .ThenBy(x => x.Name) // If the timestamp couldn't be found the clip will go to the bottom where we then order by the folder name.
        .ToList();

    partial void OnCurrentClipChanging(CamClip oldValue, CamClip newValue)
    {
        Log.Information($"Clip changed from {oldValue?.ToString() ?? "none"} to {newValue?.ToString() ?? "none"}");
    }

    partial void OnFilterTextChanged(string oldValue, string newValue)
    {
        OnPropertyChanged(nameof(Clips));
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Debug("User is selecting a folder");

        var dialog = new OpenFolderDialog();
        dialog.Title = "Select a folder containing dashcam footage";

        var result = dialog.ShowDialog();

        if (result != true)
        {
            Log.Debug("No folder was selected");
            return;
        }

        Log.Information($"Using new root folder: {dialog.FolderName}");

        // The user has committed at this point, even if it doesn't end up loading. Lets clear the current state.
        ErrorMessage = null;
        CurrentClip = null;
        _clips.Clear();
        OnPropertyChanged(nameof(Clips));

        CamStorage storage;
        try
        {
            storage = CamStorage.Map(dialog.FolderName);
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Error(ex, "Access to selected folder was denied");
            ErrorMessage = "Access to folder denied";
            return;
        }

        _clips.AddRange(storage.Clips);
        OnPropertyChanged(nameof(Clips));

        CurrentClip = Clips.FirstOrDefault();
    }
}
