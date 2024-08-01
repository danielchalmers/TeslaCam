namespace TeslaCam.Data;

public record class CamChunk
{
    public DateTime Timestamp { get; private set; }
    public IReadOnlySet<CamFile> Files { get; private set; }

    public CamChunk(DateTime timestamp, IEnumerable<CamFile> files)
    {
        Timestamp = timestamp;
        Files = files.ToHashSet();
    }

    public CamFile TryGetCamera(string name) => Files.FirstOrDefault(f => f.CameraName == name);

    public static LinkedList<CamChunk> GetChunks(string directoryPath)
    {
        var chunks = CamFile.GetClipFiles(directoryPath)
            .GroupBy(f => f.Timestamp)
            .Where(g => g.Any(x => x.CameraName == "front"))
            .OrderBy(g => g.Key)
            .Select(g => new CamChunk(g.Key, g))
            .ToList();

        var linkedList = new LinkedList<CamChunk>();

        foreach (var chunk in chunks)
        {
            linkedList.AddLast(chunk);
        }

        return linkedList;
    }

    public override string ToString() => $"{Timestamp} - {Files.Count} files";
}
