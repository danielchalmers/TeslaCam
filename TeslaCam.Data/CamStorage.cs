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
    public string FullPath { get; private init; }

    /// <summary>
    /// Every clip found recursively in the storage, ordered by timestamp.
    /// </summary>
    public IReadOnlySet<CamClip> Clips { get; private init; }

    /// <summary>
    /// Typical name of the folder that ultimately contains the dashcam clips.
    /// </summary>
    public static string ExpectedName { get; } = "TeslaCam";

    public CamStorage(string path, IEnumerable<CamClip> clips)
    {
        FullPath = Path.GetFullPath(path);
        Clips = clips.OrderByDescending(c => c.Timestamp).ToHashSet();
    }

    /// <summary>
    /// Maps out the spcefied path and constructs a repreesntation of its structure.
    /// </summary>
    public static CamStorage Traverse(string path)
    {
        var clips = CamClip.FindClips(path);
        return new CamStorage(path, clips);
    }

    public static IEnumerable<string> FindCommonRoots()
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

    public override string ToString() => $"{Clips.Count} clips ({Path.GetPathRoot(FullPath)})";
}
