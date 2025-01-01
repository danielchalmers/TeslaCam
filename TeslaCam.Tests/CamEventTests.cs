using FluentAssertions;
using TeslaCam.Data;

namespace TeslaCam.Tests;

public static class CamEventTests
{
    [Fact]
    public static void Deserializes_Correctly()
    {
        // Arrange
        var json = """
        {
            "timestamp":"2023-06-03T15:54:27",
            "city":"Taylor",
            "est_lat":"30.6075",
            "est_lon":"-97.4812",
            "reason":"user_interaction_honk",
            "camera":"0"
        }
        """;

        // Act
        var camEvent = CamEvent.Deserialize(json);

        // Assert
        camEvent.Should().NotBeNull();
        camEvent.Timestamp.Should().Be(new DateTime(2023, 6, 3, 15, 54, 27));
        camEvent.City.Should().Be("Taylor");
        camEvent.EstLat.Should().Be(30.6075m);
        camEvent.EstLon.Should().Be(-97.4812m);
        camEvent.Reason.Should().Be("user_interaction_honk");
        camEvent.Camera.Should().Be(0);
    }

    [Fact]
    public static void Deserialization_OptionalProperties()
    {
        var json = """
        {
            "timestamp":"2023-06-03T15:54:27"
        }
        """;

        var camEvent = CamEvent.Deserialize(json);

        camEvent.Should().NotBeNull();
        camEvent.Timestamp.Should().Be(new DateTime(2023, 6, 3, 15, 54, 27));
        camEvent.City.Should().BeNull();
        camEvent.EstLat.Should().Be(default);
        camEvent.EstLon.Should().Be(default);
        camEvent.Reason.Should().BeNull();
        camEvent.Camera.Should().Be(default);
    }

    [Fact]
    public static void Deserialization_TimestampRequired()
    {
        var json = """
        {
            "city":"Taylor",
            "est_lat":"30.6075",
            "est_lon":"-97.4812",
            "reason":"user_interaction_honk",
            "camera":"0"
        }
        """;

        CamEvent.Deserialize(json).Should().BeNull();
    }

    [Fact]
    public static void Deserialization_DoesNotThrowOnMalformedJson()
    {
        // Arrange
        var json = """
        {
            "timestamp":"2023T15:54:27",
            "city":"Taylor",
            "est_lat":"lat",
            "est_lon":"lon",
            "reason":"user_interaction!",
            "camera":"first"
        }
        """;

        // Act
        var camEvent = CamEvent.Deserialize(json);

        // Assert
        camEvent.Should().BeNull();
    }
}
