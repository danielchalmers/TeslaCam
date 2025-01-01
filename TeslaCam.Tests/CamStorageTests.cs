using FluentAssertions;
using TeslaCam.Data;

namespace TeslaCam.Tests;

public static class CamStorageTests
{
    [Fact]
    public static void TraverseFindsAllClips()
    {
        var storage = CamStorage.Traverse(".");

        storage.Clips.Count.Should().Be(3); // Ignores the "No Camera Files" folder.
    }

    [Theory]
    [InlineData("Mocks/2023-02-23_14-16-15", "02/23/2023 14:16:15")]
    [InlineData("Mocks/Custom Folder Name", "Custom Folder Name")]
    public static void ClipName(string path, string expectedName)
    {
        var clip = CamClip.MapClip(path);

        clip.Should().NotBeNull();
        clip.Name.Should().Be(expectedName);
    }

    [Fact]
    public static void MapClipWithNonstandardNameFallsBackToEventDataForTimestamp()
    {
        var clip = CamClip.MapClip("Mocks/Custom Folder Name");

        clip.Event.Should().NotBeNull();
        clip.Timestamp.Should().Be(clip.Event.Timestamp);
    }

    [Theory]
    [InlineData("Mocks/2023-02-23_14-16-15", 2)]
    [InlineData("Mocks/Missing Left Camera Angle on Second Chunk", 2)]
    [InlineData("Mocks/No Front Angle", 0)]
    public static void FindsAllChunks(string path, int expectedCount)
    {
        var chunks = CamClipChunk.GetChunks(path);

        chunks.Count.Should().Be(expectedCount);
    }

    [Theory]
    [InlineData("Mocks/2023-02-23_14-16-15")]
    public static void ChunksAreInCorrectOrder(string path)
    {
        var chunks = CamClipChunk.GetChunks(path);

        var node = chunks.First;
        while (node?.Next is not null)
        {
            var currentTimestamp = node.Value.Timestamp;
            var nextTimestamp = node.Next.Value.Timestamp;

            nextTimestamp.Should().BeAfter(currentTimestamp, "each timestamp should be more recent than the previous one");

            node = node.Next;
        }
    }

    [Theory]
    [InlineData("Mocks/2023-02-23_14-16-15", 8)]
    [InlineData("Mocks/Missing Left Camera Angle on Second Chunk", 7)]
    public static void FindsAllFiles(string path, int expectedCount)
    {
        var files = CamFile.FindCamFiles(path).ToList();

        files.Count.Should().Be(expectedCount);
    }
}
