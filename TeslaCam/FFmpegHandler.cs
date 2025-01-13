using System.IO;
using CliWrap;
using CliWrap.Buffered;
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
        _currentChunk = null;
        return CreateVideoForNextChunk();
    }

    public async Task<string> CreateVideoForNextChunk()
    {
        _currentChunk = _currentChunk?.Next ?? _currentClip.Chunks.First;
        await RenderChunk(_currentChunk.Value, _workingFile);
        return _workingFile;
    }

    private async Task RenderChunk(CamChunk chunk, string outputFile, string primary = "front")
    {
        if (chunk == null || !chunk.Files.Any())
            throw new ArgumentException("Chunk cannot be null or empty.", nameof(chunk));

        if (string.IsNullOrWhiteSpace(outputFile))
            throw new ArgumentException("Output file path cannot be null or empty.", nameof(outputFile));

        if (!File.Exists(chunk.Files[primary].FullPath))
            throw new ArgumentException("Primary camera file does not exist.", nameof(primary));

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

        List<string> args = [
            "-y", // Overwrite existing files
            "-i", $@"{chunk.Files[primary].FullPath}",
        ];

        void AddCam(string name)
        {
            var file = chunk.Files.GetValueOrDefault(name);

            if (file is null || file.Camera == primary)
            {
                args.Add("-f");
                args.Add("lavfi");
                args.Add("-i");
                args.Add($"color=black");
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
            "-report",
#endif
            outputFile
        ]);

        await ExecuteAsync(args);
    }

    private async Task ExecuteAsync(params IEnumerable<string> arguments)
    {
        var result = await Cli.Wrap("ffmpeg")
            .WithArguments(arguments)
            .WithWorkingDirectory(Library.FFmpegDirectory)
            .ExecuteBufferedAsync();

        if (!result.IsSuccess)
        {
            Log.Error(result.StandardError);
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
