using System.IO;
using CliWrap;
using Serilog;
using TeslaCam.Data;
using Unosquare.FFME;

namespace TeslaCam;

public class FFmpegHandler
{
    private readonly string _workingFile;
    private CamClip _currentClip;
    private LinkedListNode<CamChunk> _currentChunk;

    public FFmpegHandler()
    {
        _workingFile = Path.Combine(Path.GetTempPath(), "TeslaCam-working-file.mp4");
    }

    public Task<string> StartNewClip(CamClip clip)
    {
        _currentClip = clip;
        return CreateVideoForNextChunk();
    }

    public async Task<string> CreateVideoForNextChunk()
    {
        _currentChunk = _currentChunk?.Next ?? _currentClip.Chunks.First;
        await OverlayCamerasAsync(_currentChunk.Value, _workingFile);
        return _workingFile;
    }

    /// <summary>
    /// Overlays four camera videos on top of a main video, each labeled with its respective camera name.
    /// If a camera is missing, a half-transparent black square is displayed in its place.
    /// </summary>
    /// <param name="chunk">The CamChunk containing the files of camera videos.</param>
    /// <param name="outputFile">The output video file path.</param>
    /// <returns>Task representing the overlay process.</returns>
    private async Task OverlayCamerasAsync(CamChunk chunk, string outputFile, string selectedCamera = "front")
    {
        if (chunk == null || !chunk.Files.Any())
            throw new ArgumentException("Chunk cannot be null or empty.", nameof(chunk));

        if (string.IsNullOrWhiteSpace(outputFile))
            throw new ArgumentException("Output file path cannot be null or empty.", nameof(outputFile));

        // Common settings
        var fontPath = "C:/Windows/Fonts/SegoeUI.ttf";
        var resolution = "256x192";
        var cameraPadding = 30;

        var filterComplex = $@"
            [1:v]scale={resolution}[top_left_scaled];
            [top_left_scaled]drawtext=fontfile={fontPath}:text='Front':x=5:y=h-25:fontsize=20:fontcolor=white[top_left_labeled];
            [2:v]scale={resolution}[top_right_scaled];
            [top_right_scaled]drawtext=fontfile={fontPath}:text='Back':x=5:y=h-25:fontsize=20:fontcolor=white[top_right_labeled];
            [3:v]scale={resolution}[bottom_left_scaled];
            [bottom_left_scaled]drawtext=fontfile={fontPath}:text='Left':x=5:y=h-25:fontsize=20:fontcolor=white[bottom_left_labeled];
            [4:v]scale={resolution}[bottom_right_scaled];
            [bottom_right_scaled]drawtext=fontfile={fontPath}:text='Right':x=5:y=h-25:fontsize=20:fontcolor=white[bottom_right_labeled];

            [0:v][top_left_labeled]overlay={cameraPadding}:{cameraPadding}:shortest=1[top_left_overlay];
            [top_left_overlay][top_right_labeled]overlay=W-{resolution.Split('x')[0]}-{cameraPadding}:{cameraPadding}:shortest=1[top_right_overlay];
            [top_right_overlay][bottom_left_labeled]overlay={cameraPadding}:H-{resolution.Split('x')[1]}-{cameraPadding}:shortest=1[bottom_left_overlay];
            [bottom_left_overlay][bottom_right_labeled]overlay=W-{resolution.Split('x')[0]}-{cameraPadding}:H-{resolution.Split('x')[1]}-{cameraPadding}[output]";


        List<string> args = [
            "-y", // Overwrite existing files
            "-i", $@"{chunk.Files[selectedCamera].FullPath}",
        ];

        void AddCam(string name)
        {
            var file = chunk.Files.GetValueOrDefault(name);

            if (file is null || file.Camera == selectedCamera)
            {
                args.Add("-f");
                args.Add("lavfi");
                args.Add("-i");
                args.Add($"color=black@0.5:size={resolution}:rate=30");
            }
            else
            {
                args.Add("-i");
                args.Add($@"{file.FullPath}");
            }
        }

        AddCam("front");
        AddCam("back");
        AddCam("left_repeater");
        AddCam("right_repeater");

        args.AddRange([
            "-filter_complex", filterComplex,
            "-map", "[output]",
            "-c:v", "libx264",
            "-preset", "ultrafast",
            "-movflags", "+faststart",
#if DEBUG
            "-report", // Debugging log
#endif
            outputFile
        ]);

        await RunFFmpegProcessAsync(args);
    }

    private static async Task RunFFmpegProcessAsync(params IEnumerable<string> arguments)
    {
        var result = await Cli.Wrap("ffmpeg")
            .WithArguments(arguments)
            .WithWorkingDirectory(Library.FFmpegDirectory)
            .ExecuteAsync();

        if (!result.IsSuccess)
        {
            throw new Exception($"FFmpeg exited with code {result.ExitCode}");
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
}
