using System.IO;

namespace TeslaCam.Data;

public partial class CamStorage
{
    public string DirectoryPath { get; private set; }
    public IReadOnlySet<CamFolder> Clips { get; private set; }

    public CamStorage(string path)
    {
        DirectoryPath = path;
        Clips = CamFolder.GetClipFolders(path).ToHashSet();
    }

    /// <summary>
    /// Enumerates all USB sticks with a TeslaCam folder.
    /// </summary>
    public static IEnumerable<CamStorage> GetSticks()
    {
        var drives = DriveInfo.GetDrives();

        foreach (var drive in drives)
        {
            if (drive.DriveType == DriveType.Removable && drive.IsReady)
            {
                var teslaCamPath = Path.Combine(drive.RootDirectory.FullName, "TeslaCam");

                if (!Directory.Exists(teslaCamPath))
                {
                    continue;
                }

                yield return new CamStorage(teslaCamPath);
            }
        }
    }
}
