using System.ComponentModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using Serilog;
using TeslaCam.Data;
using TeslaCam.Processor;
using Unosquare.FFME;
using Unosquare.FFME.Common;

namespace TeslaCam;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[ObservableObject]
public partial class MainWindow : Window
{
    private readonly FFmpegHandler _ffmpeg = new();
    private readonly List<CamClip> _clips = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Clips))]
    private CamClip _currentClip;

    [ObservableProperty]
    private string _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Clips))]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private bool _isProcessing;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        PropertyChanged += OnPropertyChanged;

        LoadClips(CamStorage.FindCommonRoots());
    }

    /// <summary>
    /// A proxy for the clips list that handles ordering and filtering.
    /// </summary>
    public IReadOnlyList<CamClip> Clips => _clips
        .Where(x => x.Summary.Contains(FilterText, StringComparison.CurrentCultureIgnoreCase))
        .OrderByDescending(x => x.Timestamp) // Order newest by timestamp, either from folder name or event data.
        .ThenBy(x => x.Name) // If the timestamp couldn't be found the clip will go to the bottom where we then order by the folder name.
        .ToList();

    protected async void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CurrentClip) && Library.IsInitialized)
        {
            await MediaElement.Close();

            if (CurrentClip is not null)
            {
                IsProcessing = true;
                Log.Debug($"Starting new clip: {CurrentClip.FullPath}");
                var path = await _ffmpeg.StartNewClip(CurrentClip);
                Log.Debug($"Opening media: {path}");
                await MediaElement.Open(new Uri(path));
                await MediaElement.Play();
                IsProcessing = false;
            }
        }
    }

    private async Task<bool> TryLoadFFmpeg()
    {
        var ffmpegPaths = await PackageManager.FindFFmpegPaths();

        // Try loading all the found ffmpeg paths.
        var loaded = false;
        foreach (var ffmpegPath in ffmpegPaths)
        {
            Library.FFmpegDirectory = Path.GetDirectoryName(ffmpegPath);

            try
            {
                Library.LoadFFmpeg();
            }
            catch (FileNotFoundException ex)
            {
                Log.Debug($"FFmpeg not found at: {ex.FileName}");
            }
        }

        return loaded;
    }

    private async void Window_ContentRendered(object sender, EventArgs e)
    {
        var loaded = await TryLoadFFmpeg();

        if (!loaded)
        {
            var shouldInstall = MessageBox.Show("ffmpeg is not installed. Do you want to install it now?", "TeslaCam", MessageBoxButton.OKCancel) == MessageBoxResult.OK;

            if (!shouldInstall)
            {
                Log.Information("User didn't want to install ffmpeg");
                Close();
                return;
            }

            Log.Information("Installing ffmpeg");
            IsProcessing = true;
            loaded = await PackageManager.InstallWinGetPackage("Gyan.FFmpeg.Shared");
            IsProcessing = false;

            if (!loaded)
            {
                MessageBox.Show("Failed to install ffmpeg. Please install it manually.", "TeslaCam", MessageBoxButton.OK);
                Log.Error("Failed to install ffmpeg");
                Close();
                return;
            }
        }
    }

    private void LoadClips(params IEnumerable<string> roots)
    {
        ErrorMessage = null;
        CurrentClip = null;
        _clips.Clear();

        if (!roots.Any())
        {
            Log.Information("No roots found");
            ErrorMessage = "No TeslaCam folders found";
        }

        foreach (var root in roots)
        {
            Log.Information($"Found root folder: {root}");

            try
            {
                var storage = CamStorage.Map(root);
                _clips.AddRange(storage.Clips);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Error(ex, "Access to folder was denied");
                ErrorMessage = "Access to folder was denied";
            }
        }

        OnPropertyChanged(nameof(Clips));
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Debug("User is selecting a folder");

        var dialog = new OpenFolderDialog
        {
            Multiselect = true,
            Title = "Select a folder containing dashcam footage",
        };

        var result = dialog.ShowDialog();

        if (result != true)
        {
            Log.Debug("No folder was selected");
            return;
        }

        LoadClips(dialog.FolderNames);
    }

    private void MediaElement_MediaEnded(object sender, EventArgs e)
    {
        Log.Debug("Media: Ended");
    }

    private void MediaElement_MediaFailed(object sender, MediaFailedEventArgs e)
    {
        Log.Error(e.ErrorException, "Media: Failed");
    }
}
