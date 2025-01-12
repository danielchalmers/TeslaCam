using System.Diagnostics;
using System.Text;

namespace TeslaCam.Processor;

public class PackageInstaller
{
    private static async Task<bool> RunCommandAsync(string command, string arguments)
    {
        var outputBuilder = new StringBuilder();
        var success = false;

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine($"ERROR: {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            success = process.ExitCode == 0; 
        }
        catch
        {
            success = false;
        }

        Debug.WriteLine(outputBuilder.ToString());

        return success;
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

    public static async Task<bool> CheckIfFfmpegInstalled()
    {
        if (await RunCommandAsync("ffmpeg", "-version"))
        {
            return true;
        }

        return false;
    }
}
