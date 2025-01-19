﻿using System.Diagnostics;
using System.IO;
using Serilog;
using TeslaCam.Data;
using Unosquare.FFME;

namespace TeslaCam;

public class FFmpegHandler : IDisposable
{
    private CamClip _currentClip;
    private Process _ffmpegProcess;
    private readonly string _tempFilePath = Path.Combine(Path.GetTempPath(), $"{App.AssemblyTitle}.ts");

    public FFmpegHandler()
    {
        Uri = new Uri(_tempFilePath);
    }

    public Uri Uri { get; }

    public async Task<bool> StartNewClip(CamClip clip)
    {
        try
        {
            _currentClip = clip;
            StreamChunk(_currentClip.Chunks.First.Value);

            await Task.Delay(2000); // TODO: Implement better way to wait for ffmpeg to start and buffer.

            return await WaitForStream(_tempFilePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to start clip");
            return false;
        }
    }

    public async Task<bool> WaitForStream(string filePath, int checkInterval = 100)
    {
        while (true)
        {
            if (File.Exists(_tempFilePath) && new FileInfo(_tempFilePath).Length > 0)
                return true;

            await Task.Delay(checkInterval);
        }
    }

    private void StreamChunk(CamChunk chunk, string primary = "front")
    {
        if (chunk == null || !chunk.Files.Any())
            throw new ArgumentException("Chunk cannot be null or empty.", nameof(chunk));

        if (!File.Exists(chunk.Files[primary].FullPath))
            throw new ArgumentException("Primary camera file does not exist.", nameof(primary));

        StopFFmpeg();

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

            [0:v][top_left_labeled]overlay={cameraPadding}:{cameraPadding}[top_left_overlay];
            [top_left_overlay][top_right_labeled]overlay=W-{resolution.Split('x')[0]}-{cameraPadding}:{cameraPadding}[top_right_overlay];
            [top_right_overlay][bottom_left_labeled]overlay={cameraPadding}:H-{resolution.Split('x')[1]}-{cameraPadding}[bottom_left_overlay];
            [bottom_left_overlay][bottom_right_labeled]overlay=W-{resolution.Split('x')[0]}-{cameraPadding}:H-{resolution.Split('x')[1]}-{cameraPadding}[output]";

        List<string> args =
        [
            "-y",
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
            _tempFilePath
        ]);

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
