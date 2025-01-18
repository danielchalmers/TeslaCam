using System.Diagnostics;
using System.IO;
using Serilog;
using TeslaCam.Data;
using Unosquare.FFME;

namespace TeslaCam;

public class FFmpegHandler : IDisposable
{
    private CamClip _currentClip;
    private Process _ffmpegProcess;
    private readonly int _port = 57286;

    public FFmpegHandler()
    {
        Uri = $"udp://127.0.0.1:{_port}";
    }

    public string Uri { get; }

    public void StartNewClip(CamClip clip)
    {
        _currentClip = clip;
        StreamChunk(_currentClip.Chunks.First.Value); // TODO: Load all chunks into the stream.
    }

    private void StreamChunk(CamChunk chunk, string primary = "front")
    {
        if (chunk == null || !chunk.Files.Any())
            throw new ArgumentException("Chunk cannot be null or empty.", nameof(chunk));

        if (!File.Exists(chunk.Files[primary].FullPath))
            throw new ArgumentException("Primary camera file does not exist.", nameof(primary));

        Log.Debug($"Primary camera: {primary}");

        // Common settings
        var resolution = "256x192";
        var cameraPadding = 30;

        var filterComplex = $@"
            [1:v]scale={resolution}[top_left_scaled];
            [top_left_scaled]drawtext=text='Front':x=5:y=h-25:fontsize=20:fontcolor=white[top_left_labeled];
            [2:v]scale={resolution}[top_right_scaled];
            [top_right_scaled]drawtext=text='Back':x=5:y=h-25:fontsize=20:fontcolor=white[top_right_labeled];
            [3:v]scale={resolution}[bottom_left_scaled];
            [bottom_left_scaled]drawtext=text='Left':x=5:y=h-25:fontsize=20:fontcolor=white[bottom_left_labeled];
            [4:v]scale={resolution}[bottom_right_scaled];
            [bottom_right_scaled]drawtext=text='Right':x=5:y=h-25:fontsize=20:fontcolor=white[bottom_right_labeled];

            [0:v][top_left_labeled]overlay={cameraPadding}:{cameraPadding}:shortest=1[top_left_overlay];
            [top_left_overlay][top_right_labeled]overlay=W-{resolution.Split('x')[0]}-{cameraPadding}:{cameraPadding}:shortest=1[top_right_overlay];
            [top_right_overlay][bottom_left_labeled]overlay={cameraPadding}:H-{resolution.Split('x')[1]}-{cameraPadding}:shortest=1[bottom_left_overlay];
            [bottom_left_overlay][bottom_right_labeled]overlay=W-{resolution.Split('x')[0]}-{cameraPadding}:H-{resolution.Split('x')[1]}-{cameraPadding}:shortest=1[output]";

        List<string> args =
        [
            "-stream_loop", "-1",
            "-i", $"\"{chunk.Files[primary].FullPath}\"",
        ];

        void AddCamSquare(string name)
        {
            var file = chunk.Files.GetValueOrDefault(name);

            if (file is null || file.Camera == primary)
            {
                Log.Debug($"Adding black square for {file.Camera}");
                args.Add("-f");
                args.Add("lavfi");
                args.Add("-i");
                args.Add("color=black");
            }
            else
            {
                Log.Debug($"Adding file stream for {file.Camera}");
                args.Add("-i");
                args.Add($"\"{file.FullPath}\"");
            }
        }

        AddCamSquare("front");
        AddCamSquare("back");
        AddCamSquare("left_repeater");
        AddCamSquare("right_repeater");

        args.AddRange(
        [
            "-filter_complex", $"\"{filterComplex}\"",
            "-map", "[output]",
            "-c:v", "libx264",
            "-preset", "ultrafast",
            "-movflags", "+faststart",
#if DEBUG
        "-report",
#endif
            "-f", "mpegts",
            Uri
    ]);

        StopFFmpeg();

        _ffmpegProcess = new()
        {
            StartInfo = new()
            {
                FileName = "ffmpeg",
                Arguments = string.Join(" ", args),
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        Log.Debug($"{_ffmpegProcess.StartInfo.FileName} {_ffmpegProcess.StartInfo.Arguments}");
        _ffmpegProcess.Start();
    }

    private void StopFFmpeg()
    {
        if (_ffmpegProcess?.HasExited == false)
        {
            Log.Debug("Killing current ffmpeg process");
            _ffmpegProcess.Kill();
            _ffmpegProcess.Dispose();
            _ffmpegProcess = null;
        }
    }

    public static bool TryLoadFFmpeg()
    {
        var directories = PackageManager.FindFFmpegDirectories();

        foreach (var directory in directories)
        {
            Library.FFmpegDirectory = directory;

            Log.Debug($"Loading {directory}");
            try
            {
                var loaded = Library.LoadFFmpeg();

                if (loaded)
                {
                    Log.Information($"Loaded ffmpeg from {directory}");
                    return true;
                }
                else
                {
                    Log.Error("Failed to load ffmpeg because it was already loaded");
                    return false;
                }
            }
            catch (FileNotFoundException ex)
            {
                Log.Error(ex, "Failed to load ffmpeg");
            }
        }

        return false;
    }

    public void Dispose()
    {
        StopFFmpeg();
    }
}
