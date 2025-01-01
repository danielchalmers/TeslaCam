namespace TeslaCam.Data;

/// <summary>
/// The root folder.
/// Typically contains <c>SavedClips</c>, <c>SentryClips</c>, and <c>RecentClips</c> but that's not a requirement.
/// </summary>
public partial record class CamStorage
{
    /// <summary>
    /// The full path to the root folder.
    /// </summary>
    public string DirectoryPath { get; private init; }

    /// <summary>
    /// Every clip found recursively in the storage, ordered by timestamp.
    /// </summary>
    public IReadOnlySet<CamClip> Clips { get; private init; }

    public CamStorage(string path)
    {
        DirectoryPath = Path.GetFullPath(path);
        Clips = CamClip.GetClipFolders(path).OrderByDescending(c => c.Timestamp).ToHashSet();
    }

    /// <summary>
    /// Typical name of the folder that ultimately contains the dashcam clips.
    /// </summary>
    public static string ExpectedName { get; } = "TeslaCam";

    /// <summary>
    /// Find dashcam storage locations on USB sticks or the local folder.
    /// </summary>
    public static IReadOnlySet<CamStorage> FindStorages() =>
        FindStoragePaths().Select(path => new CamStorage(path)).ToHashSet();

    private static IEnumerable<string> FindStoragePaths()
    {
        if (Directory.Exists(ExpectedName))
        {
            yield return Path.GetFullPath(ExpectedName);
        }

        var drives = DriveInfo.GetDrives();
        foreach (var drive in drives)
        {
            var include = drive.DriveType == DriveType.Removable && drive.IsReady;

#if DEBUG
            // We only include sticks as to not traverse the entire filesystem of regular drives, but it's useful to include them while debugging.
            include = true;
#endif

            if (!include)
            {
                continue;
            }

            var expectedFolderPath = Path.Combine(drive.RootDirectory.FullName, ExpectedName);

            if (!Directory.Exists(expectedFolderPath))
            {
                continue;
            }

            yield return expectedFolderPath;
        }
    }

    public override string ToString() => $"{Clips.Count} clips ({Path.GetPathRoot(DirectoryPath)})";
}
