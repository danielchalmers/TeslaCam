using System.IO;
using System.Text.RegularExpressions;

namespace TeslaCam.Data;

public partial record class CamFile
{
    public DateTime Timestamp { get; private set; }
    public string CameraName { get; private set; }
    public string FilePath { get; private set; }

    public CamFile(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);

        var match = FileNameRegex().Match(Path.GetFileName(filePath));
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

    [GeneratedRegex(@"(?<date>\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2})-(?<camera>.+)\.mp4")]
    private static partial Regex FileNameRegex();

    public static IEnumerable<CamFile> GetClipFiles(string rootDirectory)
    {
        var files = Directory.EnumerateFiles(rootDirectory, "*", SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            var match = FileNameRegex().Match(Path.GetFileName(file));
            if (match.Success)
            {
                yield return new CamFile(file);
            }
        }
    }

    public override string ToString() => $"{Timestamp} - {CameraName}";
}
