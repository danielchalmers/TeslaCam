using System.IO;
using CliWrap;
using Serilog;
using TeslaCam.Data;
using Unosquare.FFME;

namespace TeslaCam;

public class FFmpegHandler
{
    private string _workingFile;
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
    private async Task OverlayCamerasAsync(CamChunk chunk, string outputFile)
    {
        if (chunk == null || !chunk.Files.Any())
            throw new ArgumentException("Chunk cannot be null or empty.", nameof(chunk));

        if (string.IsNullOrWhiteSpace(outputFile))
            throw new ArgumentException("Output file path cannot be null or empty.", nameof(outputFile));

        // Common settings
        var fontPath = "C:/Windows/Fonts/SegoeUI.ttf";
        var resolution = "256x192";
        var placeholderVideo = $"lavfi:color=black:size={resolution}:rate=30:duration=5";
        var cameraPadding = 30;

        // Video inputs
        var mainVideo = chunk.Files["front"].FullPath;
        var frontCam = chunk.Files.ContainsKey("front") ? chunk.Files["front"].FullPath : placeholderVideo;
        var backCam = chunk.Files.ContainsKey("back") ? chunk.Files["back"].FullPath : placeholderVideo;
        var leftCam = chunk.Files.ContainsKey("left_repeater") ? chunk.Files["left_repeater"].FullPath : placeholderVideo;
        var rightCam = chunk.Files.ContainsKey("right_repeater") ? chunk.Files["right_repeater"].FullPath : placeholderVideo;

        var filterComplex = $@"
            [1:v]scale={resolution}[front_scaled];
            [front_scaled]drawtext=fontfile={fontPath}:text='Front':x=5:y=h-30:fontsize=20:fontcolor=white[front_labeled];
            [2:v]scale={resolution}[back_scaled];
            [back_scaled]drawtext=fontfile={fontPath}:text='Back':x=5:y=h-30:fontsize=20:fontcolor=white[back_labeled];
            [3:v]scale={resolution}[left_scaled];
            [left_scaled]drawtext=fontfile={fontPath}:text='Left':x=5:y=h-30:fontsize=20:fontcolor=white[left_labeled];
            [4:v]scale={resolution}[right_scaled];
            [right_scaled]drawtext=fontfile={fontPath}:text='Right':x=5:y=h-30:fontsize=20:fontcolor=white[right_labeled];

            [0:v][front_labeled]overlay={cameraPadding}:{cameraPadding}:shortest=1[front_overlay];
            [front_overlay][back_labeled]overlay=W-{resolution.Split('x')[0]}-{cameraPadding}:{cameraPadding}:shortest=1[back_overlay];
            [back_overlay][left_labeled]overlay={cameraPadding}:H-{resolution.Split('x')[1]}-{cameraPadding}:shortest=1[left_overlay];
            [left_overlay][right_labeled]overlay=W-{resolution.Split('x')[0]}-{cameraPadding}:H-{resolution.Split('x')[1]}-{cameraPadding}[output]";

        // FFmpeg arguments
        var arguments = new List<string>
        {
            "-y", // Overwrite existing files
            "-i", mainVideo,
            "-i", frontCam,
            "-i", backCam,
            "-i", leftCam,
            "-i", rightCam,
            "-filter_complex", filterComplex,
            "-map", "[output]",
            "-c:v", "libx264",
            "-c:a", "aac",
            "-movflags", "+faststart",
            "-report", // Debugging log
            outputFile
        };

        await RunFFmpegProcessAsync(arguments);
    }

    private async Task RunFFmpegProcessAsync(params IEnumerable<string> arguments)
    {
        var result = await Cli.Wrap("ffmpeg")
            .WithArguments(arguments)
            .ExecuteAsync();

        if (!result.IsSuccess)
        {
            throw new Exception($"FFmpeg exited with code {result.ExitCode}");
        }
    }

    public bool TryLoadFFmpeg()
    {
        var ffmpegPaths = PackageManager.FindFFmpegPaths();

        var loaded = false;
        foreach (var ffmpegPath in ffmpegPaths)
        {
            Library.FFmpegDirectory = Path.GetDirectoryName(ffmpegPath);

            Log.Error($"Found ffmpeg: {ffmpegPath}");
            try
            {
                Library.LoadFFmpeg();
            }
            catch (FileNotFoundException)
            {
                Log.Error($"Couldn't load from {Library.FFmpegDirectory}");
            }
        }

        Log.Debug($"Loaded ffmpeg: {loaded}");

        return loaded;
    }
}
