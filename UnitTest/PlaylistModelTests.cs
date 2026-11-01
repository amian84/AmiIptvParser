using System.Collections.Immutable;

using AmiIptvParser;

using AwesomeAssertions;

using Xunit;

namespace UnitTest;

public sealed class PlaylistModelTests
{
    [Fact]
    public void Should_CompareEntriesByContent_When_AttributeOrderDiffers()
    {
        var left = Build(ImmutableDictionary.CreateRange(
        [
            Key("tvg-id", "n"),
            Key("group-title", "News"),
        ]));
        var right = Build(ImmutableDictionary.CreateRange(
        [
            Key("group-title", "News"),
            Key("tvg-id", "n"),
        ]));

        ReferenceEquals(left.Attributes, right.Attributes).Should().BeFalse();
        left.Should().Be(right);
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Fact]
    public void Should_RejectEntry_When_AttributeValueDiffers()
    {
        var left = Build(ImmutableDictionary.CreateRange([Key("tvg-id", "n")]));
        var right = Build(ImmutableDictionary.CreateRange([Key("tvg-id", "other")]));

        left.Should().NotBe(right);
    }

    [Fact]
    public void Should_ComparePlaylistsByContent_When_EntryCopiesDiffer()
    {
        var leftEntry = Build(ImmutableDictionary<string, string>.Empty);
        var rightEntry = Build(ImmutableDictionary<string, string>.Empty);
        var headerLeft = ImmutableDictionary.CreateRange([Key("refresh", "3600")]);
        var headerRight = ImmutableDictionary.CreateRange([Key("refresh", "3600")]);
        var left = new M3uPlaylist(ImmutableArray.Create(leftEntry), headerLeft);
        var right = new M3uPlaylist(ImmutableArray.Create(rightEntry), headerRight);

        left.Should().Be(right);
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Fact]
    public void Should_UseEmptyAttributes_When_CallerOmitsThem()
    {
        var entry = new PlaylistEntry(1, "News", new Uri("http://example.test/1"));

        entry.Attributes.Should().BeEmpty();
    }

    [Fact]
    public void Should_Throw_When_EntryTitleIsNull()
    {
        var act = () => new PlaylistEntry(1, null!, new Uri("http://example.test/1"));

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("title");
    }

    [Fact]
    public void Should_Throw_When_EntryUrlIsNull()
    {
        var act = () => new PlaylistEntry(1, "News", null!);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("url");
    }

    [Fact]
    public void Should_Throw_When_PlaylistEntriesAreDefault()
    {
        var act = () => new M3uPlaylist(default, ImmutableDictionary<string, string>.Empty);

        act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("entries");
    }

    [Fact]
    public void Should_Throw_When_HeaderAttributesAreNull()
    {
        var act = () => new M3uPlaylist(ImmutableArray<PlaylistEntry>.Empty, null!);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("headerAttributes");
    }

    private static PlaylistEntry Build(ImmutableDictionary<string, string> attributes)
        => new(1, "News", new Uri("http://example.test/1"), attributes: attributes);

    private static KeyValuePair<string, string> Key(string key, string value)
        => new(key, value);
}
