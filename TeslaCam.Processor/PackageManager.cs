using System.Diagnostics;

namespace TeslaCam.Processor;

public static class PackageManager
{
    public static async Task<Process> RunProcessAsync(string path, params IEnumerable<string> arguments)
    {
        var argumentString = string.Join(" ", arguments.Select(arg => arg.Contains(' ') ? $"\"{arg}\"" : arg));

        var processStartInfo = new ProcessStartInfo
        {
            FileName = path,
            Arguments = argumentString,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process()
        {
            StartInfo = processStartInfo
        };

        try
        {
            process.Start();

            // Capture and log FFmpeg errors
            var errorLog = await process.StandardError.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(errorLog))
            {
                Debug.WriteLine(errorLog);
            }

            await process.WaitForExitAsync();
        }
        finally
        {
            process?.Dispose();
        }

        return process;
    }

    public static async Task<bool> RunCommandAsync(string path, params IEnumerable<string> arguments)
    {
        var process = await RunProcessAsync(path, arguments);
        return process.ExitCode == 0;
    }

    public static async Task<bool> InstallWinGetPackage(string name)
    {
        // Is winget installed?
        if (!await RunCommandAsync("winget", "--version"))
        {
            return false;
        }

        return await RunCommandAsync("winget", "install " + name);
    }

    public static async Task<bool> CheckIfFFmpegInstalled()
    {
        if (await RunCommandAsync("ffmpeg", "-version"))
        {
            return true;
        }

        return false;
    }

    public static async Task<string> FindFFmpegDirectoryAsync()
    {
        var process = await RunProcessAsync("where", "ffmpeg");
        return process.StandardOutput.ToString();
    }
}
