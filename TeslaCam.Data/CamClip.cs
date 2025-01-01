using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TeslaCam.Data;

/// <summary>
/// A folder containing a collection of <see cref="CamClipChunk"/>s that make up a single continuous dashcam clip.
/// </summary>
public partial record class CamClip
{
    /// <summary>
    /// The path to the directory containing all the media files and metadata for this clip.
    /// </summary>
    public string DirectoryPath { get; private init; }

    /// <summary>
    /// The timestamp parsed from the folder name.
    /// </summary>
    public DateTime Timestamp { get; private init; }

    /// <summary>
    /// The ordered list of chunks that make up the clip as a whole.
    /// </summary>
    public LinkedList<CamClipChunk> Chunks { get; private init; }

    /// <summary>
    /// The event data associated with this clip.
    /// </summary>
    public CamEvent Event { get; private init; }

    /// <summary>
    /// The path to the thumbnail image for this clip.
    /// </summary>
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

    /// <summary>
    /// Finds all the clip folders inside the specified root directory.
    /// </summary>
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
