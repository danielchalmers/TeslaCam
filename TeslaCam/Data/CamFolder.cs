using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TeslaCam.Data;

public partial class CamFolder
{
    public DateTime Timestamp { get; private set; }
    public string DirectoryPath { get; private set; }
    public IReadOnlySet<CamFile> Files { get; private set; }
    public CamEvent Event { get; private set; }

    public CamFolder(string path)
    {
        DirectoryPath = Path.GetDirectoryName(path);

        var match = FolderNameRegex().Match(DirectoryPath);
        if (match.Success)
        {
            Timestamp = DateTime.ParseExact(match.Groups["date"].Value, "yyyy-MM-dd_HH-mm-ss", null);
        }
        else
        {
            throw new ArgumentException("Invalid folder name format");
        }

        Files = CamFile.GetClipFiles(path).ToHashSet();
        Event = GetEventData(Path.Combine(path, "event.json"));
    }

    [GeneratedRegex(@"(?<date>\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2})")]
    private static partial Regex FolderNameRegex();

    public static IEnumerable<CamFolder> GetClipFolders(string rootDirectory)
    {
        var directories = Directory.GetDirectories(rootDirectory, "*", SearchOption.TopDirectoryOnly);
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
        var json = File.ReadAllText(filePath);
        var camEvent = JsonSerializer.Deserialize<CamEvent>(json);
        return camEvent;
    }
}
