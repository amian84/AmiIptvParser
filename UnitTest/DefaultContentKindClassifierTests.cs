using AmiIptvParser;

using AwesomeAssertions;

using Xunit;

namespace UnitTest;

public sealed class DefaultContentKindClassifierTests
{
    private readonly DefaultContentKindClassifier _classifier = new();

    [Fact]
    public void Should_ReturnLive_When_UrlIsHlsPlaylist()
    {
        Classify("http://example.test/live/channel.m3u8", "News").Should().Be(ContentKind.Live);
    }

    [Fact]
    public void Should_ReturnLive_When_HlsUrlHasQueryString()
    {
        Classify("http://example.test/live.m3u8?token=abc", "News").Should().Be(ContentKind.Live);
    }

    [Fact]
    public void Should_ReturnLive_When_RelativeUrlIsHls()
    {
        Classify("live/channel.m3u8", "News").Should().Be(ContentKind.Live);
    }

    [Fact]
    public void Should_ReturnMovie_When_ExtensionIsUpperCase()
    {
        Classify("http://example.test/video.MKV", "Dune").Should().Be(ContentKind.Movie);
    }

    [Fact]
    public void Should_ReturnMovie_When_XtreamPathContainsMovie()
    {
        Classify("http://example.test/movie/user/pass/10.mp4?token=1", "Dune").Should().Be(ContentKind.Movie);
    }

    [Fact]
    public void Should_ReturnEpisode_When_XtreamPathContainsSeries()
    {
        Classify("http://example.test/series/user/pass/10.mkv", "Pilot").Should().Be(ContentKind.Episode);
    }

    [Fact]
    public void Should_ReturnEpisode_When_TitleContainsSeasonAndEpisode()
    {
        Classify("http://example.test/show.mkv", "Show S01E02").Should().Be(ContentKind.Episode);
    }

    [Fact]
    public void Should_ReturnLive_When_UrlHasNoMediaExtension()
    {
        Classify("http://example.test/live/42", "News").Should().Be(ContentKind.Live);
    }

    [Fact]
    public void Should_ReturnLive_When_LiveTitleContainsSeasonMarker()
    {
        Classify("http://example.test/live/42", "News S01E02").Should().Be(ContentKind.Live);
    }

    private ContentKind Classify(string url, string title)
        => _classifier.Classify(new PlaylistEntry
        {
            Number = 1,
            Title = title,
            Url = new Uri(url, UriKind.RelativeOrAbsolute),
        });
}
