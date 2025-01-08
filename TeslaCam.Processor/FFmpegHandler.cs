using System.Diagnostics;
using TeslaCam.Data;

namespace TeslaCam.Processor;

public class FFmpegHandler
{
    private readonly string _ffmpegPath;
    private Process _ffmpegProcess;

    public FFmpegHandler(string ffmpegExecutablePath)
    {
        if (string.IsNullOrWhiteSpace(ffmpegExecutablePath) || !File.Exists(ffmpegExecutablePath))
        {
            throw new FileNotFoundException("FFmpeg executable not found at the specified path.", ffmpegExecutablePath);
        }

        _ffmpegPath = ffmpegExecutablePath;
    }

    /// <summary>
    /// Concatenates multiple video files into a single output file.
    /// </summary>
    /// <param name="inputFiles">List of input video file paths.</param>
    /// <param name="outputFile">Path to the output video file.</param>
    public async Task ConcatenateVideosAsync(IEnumerable<string> inputFiles, string outputFile)
    {
        if (inputFiles == null || !inputFiles.Any())
            throw new ArgumentException("Input file list cannot be null or empty.", nameof(inputFiles));

        if (string.IsNullOrWhiteSpace(outputFile))
            throw new ArgumentException("Output file path cannot be null or empty.", nameof(outputFile));

        // Create temporary file list for FFmpeg
        var tempFileList = Path.GetTempFileName();
        try
        {
            using (var writer = new StreamWriter(tempFileList))
            {
                foreach (var file in inputFiles)
                {
                    if (!File.Exists(file))
                        throw new FileNotFoundException("Input file not found.", file);

                    writer.WriteLine($"file '{file.Replace("\\", "/")}'");
                }
            }

            await RunFFmpegProcessAsync(
                "-f", "concat",
                "-safe", "0",
                "-i", tempFileList.Replace("\\", "/"),
                "-c:v", "libx264",
                "-c:a", "aac",
                "-movflags", "+faststart",
                outputFile
            );
        }
        finally
        {
            File.Delete(tempFileList); // Clean up temporary file
        }
    }

    /// <summary>
    /// Overlays four camera videos on top of a main video, each labeled with its respective camera name.
    /// If a camera is missing, a half-transparent black square is displayed in its place.
    /// </summary>
    /// <param name="chunk">The CamChunk containing the files of camera videos.</param>
    /// <param name="outputFile">The output video file path.</param>
    /// <returns>Task representing the overlay process.</returns>
    public async Task OverlayCamerasAsync(CamChunk chunk, string outputFile)
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


        await RunFFmpegProcessAsync(arguments.ToArray());
    }

    /// <summary>
    /// Runs an FFmpeg process with the specified arguments.
    /// </summary>
    /// <param name="arguments">FFmpeg command-line arguments as a params array.</param>
    private async Task RunFFmpegProcessAsync(params string[] arguments)
    {
        var argumentString = string.Join(" ", arguments.Select(arg => arg.Contains(' ') ? $"\"{arg}\"" : arg));

        var processStartInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = argumentString,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _ffmpegProcess = new()
        {
            StartInfo = processStartInfo
        };

        try
        {
            _ffmpegProcess.Start();

            // Capture and log FFmpeg errors
            var errorLog = await _ffmpegProcess.StandardError.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(errorLog))
            {
                Debug.WriteLine(errorLog);
            }

            await _ffmpegProcess.WaitForExitAsync();

            if (_ffmpegProcess.ExitCode != 0)
            {
                throw new Exception($"FFmpeg process exited with code {_ffmpegProcess.ExitCode}. Check logs for details.");
            }
        }
        finally
        {
            _ffmpegProcess?.Dispose();
        }
    }
}
