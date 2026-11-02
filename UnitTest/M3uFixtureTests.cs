using System.Text;

using AmiIptvParser;

using AwesomeAssertions;

using Xunit;

namespace UnitTest;

public sealed class M3uFixtureTests
{
    [Fact]
    public void Should_KeepEntry_When_FixtureHasNoAttributes()
    {
        var entry = ParseSingle(PlaylistFixtures.NoAttributes);

        entry.Title.Should().Be("Sin grupo");
        entry.Group.Should().BeNull();
        entry.Duration.Should().BeNull();
        entry.Attributes.Should().BeEmpty();
        entry.Kind.Should().Be(ContentKind.Live);
        entry.Url.Should().Be(new Uri("http://example.test/live"));
    }

    [Fact]
    public void Should_KeepCommaInTitle_When_FixtureTitleContainsAComma()
    {
        var entry = ParseSingle(PlaylistFixtures.CommaInTitle);

        entry.Title.Should().Be("Actual, Title");
        entry.TvgName.Should().Be("Hello, World");
        entry.Group.Should().Be("A, B");
    }

    [Fact]
    public void Should_IgnoreLeadingBom_When_FixtureTextIncludesTheBom()
    {
        var bytes = PlaylistFixtures.ReadBytes(PlaylistFixtures.Bom);
        bytes[..3].Should().Equal(0xEF, 0xBB, 0xBF);

        var playlist = M3uParser.Parse(Encoding.UTF8.GetString(bytes));

        playlist.Entries.Should().ContainSingle().Which.Title.Should().Be("News");
    }

    [Fact]
    public async Task Should_IgnoreLeadingBom_When_FixtureStreamStartsWithUtf8Bom()
    {
        await using var stream = PlaylistFixtures.Open(PlaylistFixtures.Bom);

        var playlist = await M3uParser.ParseAsync(stream, TestContext.Current.CancellationToken);

        playlist.Entries.Should().ContainSingle().Which.Title.Should().Be("News");
    }

    [Fact]
    public void Should_KeepEntries_When_FixtureUsesCarriageReturn()
    {
        var bytes = PlaylistFixtures.ReadBytes(PlaylistFixtures.Crlf);
        bytes.Should().Contain((byte)'\r');

        var playlist = Parse(PlaylistFixtures.Crlf);

        playlist.Entries.Select(entry => entry.Title).Should().Equal("One", "Two");
        playlist.Entries.Select(entry => entry.Url.AbsoluteUri).Should().Equal(
            "http://example.test/one",
            "http://example.test/two");
    }

    [Fact]
    public void Should_ClassifyLive_When_FixtureIsAnM3u8Manifest()
    {
        var entry = ParseSingle(PlaylistFixtures.LiveM3u8);

        entry.Title.Should().Be("News");
        entry.Kind.Should().Be(ContentKind.Live);
        entry.Url.AbsolutePath.Should().EndWith(".m3u8");
    }

    [Fact]
    public void Should_ClassifyMovie_When_FixtureIsAnXtreamMovie()
    {
        var entry = ParseSingle(PlaylistFixtures.XtreamMovie);

        entry.Title.Should().Be("Dune");
        entry.Kind.Should().Be(ContentKind.Movie);
        entry.Url.AbsolutePath.Should().Contain("/movie/");
    }

    [Fact]
    public void Should_ClassifyEpisode_When_FixtureIsAnXtreamSeries()
    {
        var entry = ParseSingle(PlaylistFixtures.XtreamSeries);

        entry.Title.Should().Be("Pilot");
        entry.Kind.Should().Be(ContentKind.Episode);
        entry.Url.AbsolutePath.Should().Contain("/series/");
    }

    [Fact]
    public void Should_KeepCompleteEntry_When_FixtureHasPartialEntries()
    {
        var entry = ParseSingle(PlaylistFixtures.CorruptPartial);

        entry.Number.Should().Be(1);
        entry.Title.Should().Be("Kept");
        entry.Group.Should().Be("News");
        entry.Url.Should().Be(new Uri("http://example.test/kept"));
    }

    [Fact]
    public void Should_Throw_When_FixtureUriIsUnusable()
    {
        var act = () => Parse(PlaylistFixtures.CorruptUri);

        act.Should().Throw<PlaylistFormatException>();
    }

    [Fact]
    public void Should_StoreLowercaseKeys_When_FixtureAttributeKeysDifferInCase()
    {
        var playlist = Parse(PlaylistFixtures.AttributeKeyCase);

        playlist.HeaderAttributes.Should().ContainKey("url-tvg")
            .WhoseValue.Should().Be("HTTP://Example.Test/EPG.xml");
        playlist.HeaderAttributes.Should().NotContainKey("URL-TVG");
        var entry = playlist.Entries.Should().ContainSingle().Subject;

        entry.Attributes.Keys.Should().BeEquivalentTo("tvg-id", "tvg-name", "group-title");
        entry.TvgId.Should().Be("Channel.One");
        entry.TvgName.Should().Be("News");
        entry.Group.Should().Be("News");
    }

    private static M3uPlaylist Parse(string fixtureName)
        => M3uParser.Parse(PlaylistFixtures.ReadText(fixtureName));

    private static PlaylistEntry ParseSingle(string fixtureName)
        => Parse(fixtureName).Entries.Should().ContainSingle().Subject;
}
