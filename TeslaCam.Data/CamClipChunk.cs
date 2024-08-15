namespace TeslaCam.Data;

public record class CamClipChunk
{
    public DateTime Timestamp { get; private init; }
    public IReadOnlyDictionary<string, CamFile> Files { get; private init; }

    public CamClipChunk(DateTime timestamp, IEnumerable<CamFile> files)
    {
        Timestamp = timestamp;
        Files = files.ToDictionary(f => f.CameraName);
    }

    public static LinkedList<CamClipChunk> GetChunks(string directoryPath)
    {
        var chunks = CamFile.GetCamFiles(directoryPath)
            .GroupBy(f => f.Timestamp)
            .Where(g => g.Any(x => x.CameraName == "front")) // Must have a front camera clip.
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
