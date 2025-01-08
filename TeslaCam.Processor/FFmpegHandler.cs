using System.Diagnostics;

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
