using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using TeslaCam.Data;

namespace TeslaCam;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[ObservableObject]
public partial class MainWindow : Window
{
    private readonly CamStorage _camStorage;

    [ObservableProperty]
    private LinkedListNode<CamChunk> _currentChunk;

    [ObservableProperty]
    private string _errorMessage;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        _camStorage = CamStorage.GetSticks().FirstOrDefault();
        _camStorage ??= new CamStorage("./TeslaCam"); // Fall back to local directory.
        CurrentChunk = _camStorage.Clips.FirstOrDefault().Chunks.First;
    }

    public Uri MainMediaSource => GetCameraFeed("front");
    public Uri BottomLeftMediaSource => GetCameraFeed("left_repeater");
    public Uri BottomRightMediaSource => GetCameraFeed("right_repeater");

    private Uri GetCameraFeed(string name) => new(CurrentChunk.Value.TryGetCamera(name)?.FilePath);

    private void MainMedia_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        ErrorMessage = "Error loading media: " + e.ErrorException.Message;
    }

    private void MainMedia_MediaEnded(object sender, RoutedEventArgs e)
    {
        CurrentChunk = CurrentChunk.Next;
        OnPropertyChanged(nameof(MainMediaSource));
        OnPropertyChanged(nameof(BottomLeftMediaSource));
        OnPropertyChanged(nameof(BottomRightMediaSource));
    }
}
