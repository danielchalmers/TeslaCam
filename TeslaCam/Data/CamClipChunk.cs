namespace TeslaCam.Data;

public record class CamClipChunk
{
    public DateTime Timestamp { get; private set; }
    public IReadOnlySet<CamFile> Files { get; private set; }

    public CamClipChunk(DateTime timestamp, IEnumerable<CamFile> files)
    {
        Timestamp = timestamp;
        Files = files.ToHashSet();
    }

    public CamFile TryGetCamera(string name) => Files.FirstOrDefault(f => f.CameraName == name);

    public static LinkedList<CamClipChunk> GetChunks(string directoryPath)
    {
        var chunks = CamFile.GetClipFiles(directoryPath)
            .GroupBy(f => f.Timestamp)
            .Where(g => g.Any(x => x.CameraName == "front"))
            .OrderBy(g => g.Key)
            .Select(g => new CamClipChunk(g.Key, g))
            .ToList();

        var linkedList = new LinkedList<CamClipChunk>();

        foreach (var chunk in chunks)
        {
            linkedList.AddLast(chunk);
        }

        return linkedList;
    }

    public override string ToString() => $"{Timestamp} - {Files.Count} files";
}
