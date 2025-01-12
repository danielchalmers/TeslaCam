using System.IO;
using System.IO.Compression;
using System.Net.Http;
using Serilog;

namespace TeslaCam;

public static class PackageManager
{
    private static async Task DownloadFile(string url, string savePath)
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            using var fileStream = File.Create(savePath);
            await response.Content.CopyToAsync(fileStream);
        }
    }

    private static void ExtractZipFile(string zipFilePath, string extractPath)
    {
        ZipFile.ExtractToDirectory(zipFilePath, extractPath, true);
    }

    public static async Task DownloadAndExtractFFmpeg()
    {
        var outputFolder = "ffmpeg";
        var url = "https://github.com/BtbN/FFmpeg-Builds/releases/latest/download/ffmpeg-master-latest-win64-gpl-shared.zip"; // TODO: ARM64 builds?
        var tempPath = Path.GetTempFileName();

        Log.Information("Getting ffmpeg");

        Log.Debug($"Downloading ffmpeg to {tempPath} from {url}");
        await DownloadFile(url, tempPath);

        Log.Debug($"Extracting ffmpeg to {outputFolder}");
        ExtractZipFile(tempPath, outputFolder);

        File.Delete(tempPath);
    }

    public static IEnumerable<string> FindFFmpegPaths()
    {
        foreach (var path in Directory.EnumerateFiles(".", "ffmpeg.exe", SearchOption.AllDirectories))
        {
            yield return Path.GetFullPath(Path.GetDirectoryName(path));
        }
    }
}
