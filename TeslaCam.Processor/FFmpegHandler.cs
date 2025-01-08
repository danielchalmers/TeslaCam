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
    /// Applies an overlay video on top of a background video.
    /// </summary>
    /// <param name="backgroundFile">Path to the background video file.</param>
    /// <param name="overlayFile">Path to the overlay video file.</param>
    /// <param name="outputFile">Path to the output video file.</param>
    /// <param name="xOffset">X offset of the overlay video.</param>
    /// <param name="yOffset">Y offset of the overlay video.</param>
    public async Task ApplyOverlayAsync(string backgroundFile, string overlayFile, string outputFile, int xOffset = 0, int yOffset = 0)
    {
        if (string.IsNullOrWhiteSpace(backgroundFile) || !File.Exists(backgroundFile))
            throw new FileNotFoundException("Background video file not found.", backgroundFile);

        if (string.IsNullOrWhiteSpace(overlayFile) || !File.Exists(overlayFile))
            throw new FileNotFoundException("Overlay video file not found.", overlayFile);

        if (string.IsNullOrWhiteSpace(outputFile))
            throw new ArgumentException("Output file path cannot be null or empty.", nameof(outputFile));

        await RunFFmpegProcessAsync(
            "-i", backgroundFile,
            "-i", overlayFile,
            "-filter_complex", $"[0:v][1:v]overlay={xOffset}:{yOffset}",
            "-c:v", "libx264",
            "-c:a", "aac",
            outputFile
        );
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

        // Default placeholders for missing cameras
        var mainVideo = chunk.Files["front"].FullPath;
        var placeholderVideo = "lavfi:color=black:size=320x240:rate=30:duration=5";
        var frontCam = chunk.Files.ContainsKey("front") ? chunk.Files["front"].FullPath : placeholderVideo;
        var backCam = chunk.Files.ContainsKey("back") ? chunk.Files["back"].FullPath : placeholderVideo;
        var leftCam = chunk.Files.ContainsKey("left_repeater") ? chunk.Files["left_repeater"].FullPath : placeholderVideo;
        var rightCam = chunk.Files.ContainsKey("right_repeater") ? chunk.Files["right_repeater"].FullPath : placeholderVideo;

        // FFmpeg filter complex for overlaying cameras
        var filterComplex = $@"
            [1:v]scale=320:240[front_scaled];
            [2:v]scale=320:240[back_scaled];
            [3:v]scale=320:240[left_scaled];
            [4:v]scale=320:240[right_scaled];
            [0:v][front_scaled]overlay=10:10:shortest=1[front];
            [front][back_scaled]overlay=W-w-10:10:shortest=1[back];
            [back][left_scaled]overlay=10:H-h-10:shortest=1[left];
            [left][right_scaled]overlay=W-w-10:H-h-10:shortest=1[final];
            [final]drawtext=fontfile=C:/WINDOWS/fonts/arial.ttf:text='Front':x=15:y=15:fontsize=24:fontcolor=white:box=1:boxcolor=black@0.5[final_with_front];
            [final_with_front]drawtext=fontfile=C:/WINDOWS/fonts/arial.ttf:text='Back':x=w-100:y=15:fontsize=24:fontcolor=white:box=1:boxcolor=black@0.5[final_with_back];
            [final_with_back]drawtext=fontfile=C:/WINDOWS/fonts/arial.ttf:text='Left':x=15:y=h-50:fontsize=24:fontcolor=white:box=1:boxcolor=black@0.5[final_with_left];
            [final_with_left]drawtext=fontfile=C:/WINDOWS/fonts/arial.ttf:text='Right':x=w-100:y=h-50:fontsize=24:fontcolor=white:box=1:boxcolor=black@0.5[output]";

        var arguments = new List<string>
        {
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
            "-report",
            outputFile
        };

        await RunFFmpegProcessAsync(arguments.ToArray());
    }

    /// <summary>
    /// Streams a concatenated set of video files to an HTTP endpoint.
    /// </summary>
    /// <param name="inputFiles">List of input video file paths.</param>
    /// <param name="outputUri">HTTP endpoint URI for streaming.</param>
    public async Task StreamToHttpAsync(IEnumerable<string> inputFiles, string outputUri)
    {
        if (inputFiles == null || !inputFiles.Any())
            throw new ArgumentException("Input file list cannot be null or empty.", nameof(inputFiles));

        if (string.IsNullOrWhiteSpace(outputUri))
            throw new ArgumentException("Output URI cannot be null or empty.", nameof(outputUri));

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
                "-f", "mpegts",
                outputUri
            );
        }
        finally
        {
            // Clean up temporary file
            File.Delete(tempFileList);
        }
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
