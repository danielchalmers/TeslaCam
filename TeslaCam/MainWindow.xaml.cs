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

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        _camStorage = CamStorage.GetSticks().FirstOrDefault();
        _camStorage ??= new CamStorage("./TeslaCam"); // Always fall back to local directory.
        CurrentChunk = _camStorage.Clips.FirstOrDefault().Chunks.First;
    }


    public Uri MainMediaSource => new(CurrentChunk.Value.TryGetCamera("front")?.FilePath);
    public Uri BottomLeftMediaSource => new(CurrentChunk.Value.TryGetCamera("left_repeater")?.FilePath);
    public Uri BottomRightMediaSource => new(CurrentChunk.Value.TryGetCamera("right_repeater")?.FilePath);

    private void MainMedia_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        MainMedia.Visibility = Visibility.Collapsed;
        ErrorMessage.Text = "Error loading media: " + e.ErrorException.Message;
        ErrorMessage.Visibility = Visibility.Visible;
    }

    private void MainMedia_MediaEnded(object sender, RoutedEventArgs e)
    {
        CurrentChunk = CurrentChunk.Next;
        OnPropertyChanged(nameof(MainMediaSource));
        OnPropertyChanged(nameof(BottomLeftMediaSource));
        OnPropertyChanged(nameof(BottomRightMediaSource));
    }
}
