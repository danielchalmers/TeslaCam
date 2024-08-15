﻿namespace TeslaCam.Data;

public partial record class CamStorage
{
    public string DirectoryPath { get; private init; }
    public IReadOnlySet<CamClip> Clips { get; private init; }

    public CamStorage(string path)
    {
        DirectoryPath = Path.GetFullPath(path);
        Clips = CamClip.GetClipFolders(path).ToHashSet();
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

    public override string ToString() => $"{Clips.Count} clips ({Path.GetPathRoot(DirectoryPath)})";
}
