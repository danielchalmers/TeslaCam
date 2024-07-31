using System.Windows;
using TeslaCam.Data;

namespace TeslaCam;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        //var camStorage = CamStorage.GetSticks().FirstOrDefault();
        var camStorage = new CamStorage("./TeslaCam"); // Always check local directory.
        Console.WriteLine(camStorage.DirectoryPath);
        CurrentChunk = camStorage.Clips.FirstOrDefault().Chunks[1];
    }

    public CamChunk CurrentChunk { get; set; }

    public Uri MainMediaSource => new(CurrentChunk.TryGetCamera("front")?.FilePath);
    public Uri BottomLeftMediaSource => new(CurrentChunk.TryGetCamera("left_repeater")?.FilePath);
    public Uri BottomRightMediaSource => new(CurrentChunk.TryGetCamera("right_repeater")?.FilePath);
}
