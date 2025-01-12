using System.IO;
using CliWrap;
using Serilog;

namespace TeslaCam;

public static class PackageManager
{
    public static async Task<bool> InstallWinGetPackage(string name)
    {
        Log.Information($"Installing {name} using winget...");
        var wingetInstalled = (await Cli.Wrap("winget")
            .WithArguments("--version")
            .ExecuteAsync())
            .IsSuccess;

        if (!wingetInstalled)
        {
            Log.Error($"winget is not installed");
            return false;
        }

        var installResult = await Cli.Wrap("winget")
            .WithArguments(["install", name])
            .ExecuteAsync();

        Log.Information($"Installed: {installResult}");

        return installResult.IsSuccess;
    }

    public static Task<bool> InstallFFmpeg() => InstallWinGetPackage("Gyan.FFmpeg.Shared");

    private static IEnumerable<string> FindFilePathsFromEnvironmentVariables(string fileName)
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");

        foreach (var directory in pathVariable.Split(Path.PathSeparator))
        {
            var fullPath = Path.Combine(directory, fileName);

            if (File.Exists(fullPath))
            {
                yield return fullPath;
            }
        }
    }

    public static IEnumerable<string> FindFFmpegPaths() => FindFilePathsFromEnvironmentVariables("ffmpeg.exe");
}
