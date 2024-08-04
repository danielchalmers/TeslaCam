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
        CurrentClip = _camStorage.Clips.FirstOrDefault();
    }

    public Uri BottomLeftSource => GetCameraFeed("left_repeater");
    public Uri BottomRightSource => GetCameraFeed("right_repeater");

    private Uri GetCameraFeed(string name) => new(CurrentClip.CurrentChunk.Value.TryGetCamera(name).FilePath);

    private void StageView_FileStarted(object sender, EventArgs e)
    {
        OnPropertyChanged(nameof(BottomLeftSource));
        OnPropertyChanged(nameof(BottomRightSource));
    }
}
