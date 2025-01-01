﻿using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TeslaCam.Data;

public partial record class CamClip
{
    public string DirectoryPath { get; private init; }
    public DateTime Timestamp { get; private init; }
    public LinkedList<CamClipChunk> Chunks { get; private init; }
    public CamEvent Event { get; private init; }
    public string ThumbnailPath { get; private init; }

    public CamClip(string path)
    {
        DirectoryPath = Path.GetFullPath(path);

        var match = FolderNameRegex().Match(DirectoryPath);
        if (!match.Success)
        {
            throw new ArgumentException("Invalid folder name format");
        }

        Timestamp = DateTime.ParseExact(match.Groups["date"].Value, "yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
        Chunks = CamClipChunk.GetChunks(DirectoryPath);
        Event = GetEventData(Path.Combine(DirectoryPath, "event.json"));
        ThumbnailPath = Path.Combine(DirectoryPath, "thumb.png");
    }

    [GeneratedRegex(@"(?<date>\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2})")]
    private static partial Regex FolderNameRegex();

    public static IEnumerable<CamClip> GetClipFolders(string rootDirectory)
    {
        var directories = Directory.GetDirectories(rootDirectory, "*", SearchOption.AllDirectories);
        foreach (var directory in directories)
        {
            var match = FolderNameRegex().Match(Path.GetFileName(directory));
            if (match.Success)
            {
                yield return new(directory);
            }
        }
    }

    public static CamEvent GetEventData(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var json = File.ReadAllText(filePath);
        return CamEvent.Deserialize(json);
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(Timestamp);

        if (Event is not null)
        {
            builder.AppendLine();
            builder.Append(Event.City);
        }

        return builder.ToString();
    }
}
