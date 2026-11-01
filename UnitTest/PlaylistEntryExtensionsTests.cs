using AmiIptvParser;

using AwesomeAssertions;

using Xunit;

namespace UnitTest;

public sealed class PlaylistEntryExtensionsTests
{
    private static readonly IReadOnlyList<PlaylistEntry> Entries =
    [
        BuildEntry(1, "News", "News", ContentKind.Live),
        BuildEntry(2, "Dune", "Movies", ContentKind.Movie),
        BuildEntry(3, "Pilot", "Shows", ContentKind.Episode),
        BuildEntry(4, "Sports", "News", ContentKind.Live),
        BuildEntry(5, "Untitled", null, ContentKind.Live),
    ];

    [Fact]
    public void Should_ReturnLiveEntries_When_KindsAreMixed()
    {
        Entries.Live().Select(entry => entry.Title).Should().Equal("News", "Sports", "Untitled");
    }

    [Fact]
    public void Should_ReturnMovies_When_KindsAreMixed()
    {
        Entries.Movies().Select(entry => entry.Title).Should().Equal("Dune");
    }

    [Fact]
    public void Should_ReturnEpisodes_When_KindsAreMixed()
    {
        Entries.Episodes().Select(entry => entry.Title).Should().Equal("Pilot");
    }

    [Fact]
    public void Should_ReturnDistinctGroups_When_EntriesRepeatGroups()
    {
        Entries.Groups().Should().Equal("News", "Movies", "Shows");
    }

    [Fact]
    public void Should_FilterByGroup_When_GroupIsRequested()
    {
        Entries.InGroup("News").Select(entry => entry.Title).Should().Equal("News", "Sports");
    }

    [Fact]
    public void Should_Throw_When_GroupIsBlank()
    {
        var act = () => Entries.InGroup("  ").ToList();

        act.Should().Throw<ArgumentException>();
    }

    private static PlaylistEntry BuildEntry(int number, string title, string? group, ContentKind kind)
        => new(number, title, new Uri($"http://example.test/{number}"), group: group, kind: kind);
}
