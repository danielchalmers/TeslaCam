using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using Serilog;
using TeslaCam.Data;
using Unosquare.FFME;
using Unosquare.FFME.Common;

namespace TeslaCam;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[ObservableObject]
public partial class MainWindow : Window
{
    private readonly List<ClipStream> _clips = [];

    [ObservableProperty]
    private ClipStream _currentStream;

    [ObservableProperty]
    private string _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Clips))]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private int _busyCount;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        LoadClips(CamStorage.FindCommonRoots());
    }

    /// <summary>
    /// A proxy for the clips list that handles ordering and filtering.
    /// </summary>
    public IReadOnlyList<ClipStream> Clips => _clips
        .Where(x => x.Clip.Summary.Contains(FilterText, StringComparison.CurrentCultureIgnoreCase))
        .OrderByDescending(x => x.Clip.Timestamp) // Order newest by timestamp, either from folder name or event data.
        .ThenBy(x => x.Clip.Name) // If the timestamp couldn't be found the clip will go to the bottom where we then order by the folder name.
        .ToList();

    partial void OnCurrentStreamChanging(ClipStream oldValue, ClipStream newValue)
    {
        BusyCount++;
        ErrorMessage = null;

        Task.Run(async () =>
        {
            try
            {
                await MediaElement.Close();

                if (oldValue is not null)
                {
                    await oldValue.StopStream();
                }

                if (CurrentStream is not null && Library.IsInitialized)
                {
                    ErrorMessage = await LoadClip(CurrentStream);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to switch streams");
            }
            finally
            {
                BusyCount--;
            }
        });
    }

    partial void OnErrorMessageChanged(string value)
    {
        if (value is not null)
        {
            Log.Error(value);
        }
    }

    private async Task<string> LoadClip(ClipStream stream)
    {
        Log.Debug($"Loading clip from {stream.Clip.FullPath}");
        var result = await stream.StartStream();

        if (!result)
            return "Failed to render clip";

        result = await MediaElement.Open(stream.Uri);

        if (!result)
            return "Failed to open clip";

        result = await MediaElement.Play();

        if (!result)
            return "Failed to play clip";

        return null;
    }

    private async void Window_ContentRendered(object sender, EventArgs e)
    {
        var loaded = ClipStream.TryLoadFFmpeg();

        if (!loaded)
        {
            var shouldInstall = MessageBox.Show(this, "You need ffmpeg to play clips. Download it now?", App.Title, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK;

            if (shouldInstall)
            {
                BusyCount++;

                try
                {
                    await PackageManager.DownloadAndExtractFFmpeg();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Failed to download ffmpeg: {ex.Message}", App.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }

                loaded = ClipStream.TryLoadFFmpeg();

                BusyCount--;

                if (!loaded)
                {
                    MessageBox.Show(this, "Failed to load ffmpeg. You won't be able to play clips.", App.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                Log.Error("User didn't want to download ffmpeg");
            }
        }
    }

    private void LoadClips(params IEnumerable<string> roots)
    {
        ErrorMessage = null;
        CurrentStream = null;
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
                _clips.AddRange(storage.Clips.Select(c => new ClipStream(c)));
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

    private void MediaElement_MediaInitializing(object sender, MediaInitializingEventArgs e)
    {
    }

    private void MediaElement_MediaOpening(object sender, MediaOpeningEventArgs e)
    {
        Log.Debug($"Media Opening {e.Info.MediaSource}");
    }

    private void MediaElement_MediaOpened(object sender, MediaOpenedEventArgs e)
    {
    }

    private void MediaElement_MediaEnded(object sender, EventArgs e)
    {
        Log.Debug("Media Ended");
    }

    private void MediaElement_MediaFailed(object sender, MediaFailedEventArgs e)
    {
        Log.Error(e.ErrorException, "Media Failed");
    }

    private void MediaElement_BufferingStarted(object sender, EventArgs e)
    {
    }

    private void MediaElement_BufferingEnded(object sender, EventArgs e)
    {
    }

    private async void Window_Closing(object sender, CancelEventArgs e)
    {
        await MediaElement.Close();

        if (CurrentStream is not null)
        {
            await CurrentStream.DisposeAsync();
        }
    }
}
