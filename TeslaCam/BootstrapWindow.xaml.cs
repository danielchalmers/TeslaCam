using System.IO;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using TeslaCam.Processor;
using Unosquare.FFME;

namespace TeslaCam;

/// <summary>
/// Interaction logic for BootstrapWindow.xaml
/// </summary>
[ObservableObject]
public partial class BootstrapWindow : Window
{
    [ObservableProperty]
    private BootstrapState _state = new BootstrapStateInitializing();

    public BootstrapWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
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
            catch (FileNotFoundException)
            {
                Log.Debug($"FFmpeg not found at: {ffmpegPath}", ffmpegPath);
            }
        }

        // If none of the paths worked, we'll try to install it.
        if (!loaded)
        {
            var shouldInstall = MessageBox.Show("ffmpeg is not installed. Do you want to install it now?", "TeslaCam", MessageBoxButton.OKCancel) == MessageBoxResult.OK;

            if (!shouldInstall)
            {
                Log.Information("User didn't want to install ffmpeg");
                Close();
                return;
            }
        }

        Visibility = Visibility.Visible;
    }

    [RelayCommand]
    private async Task InstallDependencies()
    {

        Log.Information("Installing ffmpeg");
        var loaded = await PackageManager.InstallWinGetPackage("Gyan.FFmpeg.Shared");

        if (!loaded)
        {
            MessageBox.Show("Failed to install ffmpeg. Please install it manually.", "TeslaCam", MessageBoxButton.OK);
            Log.Error("Failed to install ffmpeg");
            Close();
            return;
        }
    }

    private async void Window_ContentRendered(object sender, EventArgs e)
    {
        await Task.Delay(1000);
        var mainWindow = new MainWindow();
        mainWindow.Show();
        Close();
    }
}

public abstract class BootstrapState : ObservableObject
{
    public abstract string Text { get; }
}

public class BootstrapStateInitializing : BootstrapState
{
    public override string Text => "Initializing...";
}

public class BootstrapStateMissingDependencies : BootstrapState
{
    public override string Text => "Missing Dependencies. Do you want to install them now?";

    public ICommand InstallCommand { get; }
    public ICommand DeclineCommand { get; }

    public BootstrapStateMissingDependencies(ICommand installCommand, ICommand declineCommand)
    {
        InstallCommand = installCommand;
        DeclineCommand = declineCommand;
    }
}

public class BootstrapStateInstalling : BootstrapState
{
    public override string Text => "Installing...";

    public ICommand CancelCommand { get; }

    public BootstrapStateInstalling(ICommand cancelCommand)
    {
        CancelCommand = cancelCommand;
    }
}

public class BootstrapStateError : BootstrapState
{
    public override string Text => "Something went wrong.";

    public ICommand CloseCommand { get; }

    public BootstrapStateError(ICommand closeCommand)
    {
        CloseCommand = closeCommand;
    }
}
