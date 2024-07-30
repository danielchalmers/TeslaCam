using System.IO;
using System.Text.RegularExpressions;

namespace TeslaCam.Data;

public partial class CamFile
{
    public DateTime Timestamp { get; private set; }
    public string CameraName { get; private set; }
    public string FileName { get; private set; }
    public string Directory { get; private set; }

    public CamFile(string filePath)
    {
        FileName = Path.GetFileName(filePath);
        Directory = Path.GetDirectoryName(filePath);

        var match = FileNameRegex().Match(FileName);
        if (match.Success)
        {
            Timestamp = DateTime.ParseExact(match.Groups["date"].Value, "yyyy-MM-dd_HH-mm-ss", null);
            CameraName = match.Groups["camera"].Value;
        }
        else
        {
            throw new ArgumentException("Invalid file name format");
        }
    }

    [GeneratedRegex(@"(?<date>\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2})-(?<camera>.+)")]
    private static partial Regex FileNameRegex();
}
