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

    public static IEnumerable<CamChunk> GetChunks(string directoryPath)
    {
        return CamFile.GetClipFiles(directoryPath)
                .GroupBy(f => f.Timestamp)
                .Select(g => new CamChunk(g.Key, g));
    }

    public override string ToString() => $"{Timestamp} - {Files.Count} files";
}
