using System.Windows;
using Serilog;

namespace TeslaCam;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static string Title { get; } = "Sentry Replay";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Verbose()
            .WriteTo.Debug()
            .CreateLogger();

        Log.Information("Application starting...");
    }
}
