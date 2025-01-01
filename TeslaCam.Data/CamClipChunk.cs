namespace TeslaCam.Data;

/// <summary>
/// All the dashcam clips that were recorded at the same time from different angles.
/// Multiple of these can be grouped into a <see cref="CamClip"/>.
/// </summary>
public record class CamClipChunk
{
    public DateTime Timestamp { get; private init; }
    public IReadOnlyDictionary<string, CamFile> Files { get; private init; }

    public CamClipChunk(DateTime timestamp, IEnumerable<CamFile> files)
    {
        Timestamp = timestamp;
        Files = files.ToDictionary(f => f.Camera);
    }

    /// <summary>
    /// Find all the media files in the directory and group them by timestamp into chunks.
    /// </summary>
    public static LinkedList<CamClipChunk> GetChunks(string directoryPath)
    {
        var chunks = CamFile.FindCamFiles(directoryPath)
            .GroupBy(f => f.Timestamp)
            .Where(g => g.Any(x => x.Camera == "front")) // Must have a front camera clip to be a valid chunk.
            .OrderBy(g => g.Key)
            .Select(g => new CamClipChunk(g.Key, g));

        var linkedList = new LinkedList<CamClipChunk>();

        foreach (var chunk in chunks)
        {
            linkedList.AddLast(chunk);
        }

        return linkedList;
    }

    public override string ToString() => $"{Files.Count} files";
}
