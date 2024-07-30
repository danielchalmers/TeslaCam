using System.IO;
using System.Text.RegularExpressions;

namespace TeslaCam.Data;

public partial class CamFolder
{
    public DateTime Timestamp { get; private set; }
    public string Directory { get; private set; }

    public CamFolder(string path)
    {
        Directory = Path.GetDirectoryName(path);

        var match = FolderNameRegex().Match(Directory);
        if (match.Success)
        {
            Timestamp = DateTime.ParseExact(match.Groups["date"].Value, "yyyy-MM-dd_HH-mm-ss", null);
        }
        else
        {
            throw new ArgumentException("Invalid folder name format");
        }
    }

    [GeneratedRegex(@"(?<date>\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2})")]
    private static partial Regex FolderNameRegex();
}
