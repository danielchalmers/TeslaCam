using CliWrap;
using CliWrap.Buffered;

namespace TeslaCam.Processor;

public static class PackageManager
{
    public static async Task<bool> InstallWinGetPackage(string name)
    {
        var wingetInstalled = (await Cli.Wrap("winget")
            .WithArguments("--version")
            .ExecuteAsync())
            .IsSuccess;

        if (!wingetInstalled)
        {
            return false;
        }

        var installResult = await Cli.Wrap("winget")
            .WithArguments(["install", name])
            .ExecuteAsync();

        return installResult.IsSuccess;
    }

    public static async Task<bool> CheckIfFFmpegInstalled()
    {
        return (await Cli.Wrap("ffmpeg")
            .WithArguments("-version")
            .ExecuteAsync())
            .IsSuccess;
    }

    public static async Task<string[]> FindFFmpegPaths()
    {
        var result = await Cli.Wrap("where")
            .WithArguments("ffmpeg")
            .ExecuteBufferedAsync();

        return result.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    }
}
