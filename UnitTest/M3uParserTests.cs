using System.Text;

using AmiIptvParser;

using AwesomeAssertions;

using Xunit;

namespace UnitTest;

public sealed class M3uParserTests
{
    [Fact]
    public void Should_KeepEntry_When_ExtInfHasNoAttributes()
    {
        var playlist = M3uParser.Parse("""
            #EXTM3U
            #EXTINF:-1,Sin grupo
            http://example.test/live
            """);

        var entry = playlist.Entries.Should().ContainSingle().Subject;
        entry.Number.Should().Be(1);
        entry.Title.Should().Be("Sin grupo");
        entry.Group.Should().BeNull();
        entry.Duration.Should().BeNull();
        entry.Attributes.Should().BeEmpty();
        entry.Kind.Should().Be(ContentKind.Live);
        entry.Url.Should().Be(new Uri("http://example.test/live"));
    }

    [Fact]
    public void Should_KeepCommaInTitle_When_AttributesAlsoContainCommas()
    {
        var playlist = M3uParser.Parse("""
            #EXTM3U
            #EXTINF:-1 tvg-name="Hello, World" group-title="A, B",Actual, Title
            http://example.test/a
            """);

        var entry = playlist.Entries.Should().ContainSingle().Subject;
        entry.Title.Should().Be("Actual, Title");
        entry.TvgName.Should().Be("Hello, World");
        entry.Group.Should().Be("A, B");
    }

    [Fact]
    public void Should_ReadHeaderAttributes_When_ExtM3uDeclaresThem()
    {
        var playlist = M3uParser.Parse("""
            #EXTM3U url-tvg="http://example.test/epg.xml" refresh="3600"
            #EXTINF:-1,News
            http://example.test/news
            """);

        playlist.HeaderAttributes.Should().ContainKey("url-tvg")
            .WhoseValue.Should().Be("http://example.test/epg.xml");
        playlist.HeaderAttributes["refresh"].Should().Be("3600");
    }

    [Fact]
    public void Should_ApplyExtGrp_When_GroupTitleIsAbsent()
    {
        var playlist = M3uParser.Parse("""
            #EXTM3U
            #EXTGRP:News
            #EXTINF:-1,Channel
            http://example.test/c
            """);

        playlist.Entries.Should().ContainSingle().Which.Group.Should().Be("News");
    }

    [Fact]
    public void Should_PreferGroupTitle_When_ExtGrpAlsoPresent()
    {
        var playlist = M3uParser.Parse("""
            #EXTM3U
            #EXTGRP:News
            #EXTINF:-1 group-title="Sports",Channel
            http://example.test/c
            """);

        playlist.Entries.Should().ContainSingle().Which.Group.Should().Be("Sports");
    }

    [Fact]
    public void Should_ReadDuration_When_ExtInfDeclaresSeconds()
    {
        var playlist = M3uParser.Parse("""
            #EXTM3U
            #EXTINF:90,Film
            http://example.test/film.mkv
            """);

        var entry = playlist.Entries.Should().ContainSingle().Subject;
        entry.Duration.Should().Be(TimeSpan.FromSeconds(90));
        entry.Kind.Should().Be(ContentKind.Movie);
    }

    [Fact]
    public void Should_NumberEntriesFromOne_When_PlaylistHasTwoEntries()
    {
        var playlist = M3uParser.Parse("""
            #EXTM3U
            #EXTINF:-1,One
            http://example.test/one
            #EXTINF:-1,Two
            http://example.test/two
            """);

        playlist.Entries.Select(entry => entry.Number).Should().Equal(1, 2);
        playlist.Entries.Select(entry => entry.Title).Should().Equal("One", "Two");
    }

    [Fact]
    public void Should_KeepEntry_When_LinesUseCarriageReturn()
    {
        var playlist = M3uParser.Parse("#EXTM3U\r\n#EXTINF:-1,Sin grupo\r\nhttp://example.test/live\r\n");

        playlist.Entries.Should().ContainSingle().Which.Title.Should().Be("Sin grupo");
    }

    [Fact]
    public void Should_ParseEntry_When_TagsUseLowerCase()
    {
        var playlist = M3uParser.Parse("""
            #extm3u
            #extinf:-1,News
            http://example.test/news
            """);

        playlist.Entries.Should().ContainSingle().Which.Title.Should().Be("News");
    }

    [Fact]
    public void Should_ReturnEmptyPlaylist_When_ContentIsOnlyHeader()
    {
        var playlist = M3uParser.Parse("#EXTM3U\n");

        playlist.Entries.Should().BeEmpty();
        playlist.HeaderAttributes.Should().BeEmpty();
    }

    [Fact]
    public void Should_ParseEntries_When_TextReaderIsUsed()
    {
        using var reader = new StringReader("#EXTINF:-1,News\nhttp://example.test/news\n");

        var playlist = M3uParser.Parse(reader);

        playlist.Entries.Should().ContainSingle().Which.Title.Should().Be("News");
    }

    [Fact]
    public async Task Should_ParseEntries_When_StreamStartsWithUtf8Bom()
    {
        var playlist = await M3uParser.ParseAsync(
            StreamOf(Encoding.UTF8, "#EXTINF:-1,News\nhttp://example.test/a\n"),
            TestContext.Current.CancellationToken);

        playlist.Entries.Should().ContainSingle().Which.Title.Should().Be("News");
    }

    [Fact]
    public async Task Should_ParseEntries_When_StreamIsUtf16()
    {
        var playlist = await M3uParser.ParseAsync(
            StreamOf(Encoding.Unicode, "#EXTINF:-1,News\nhttp://example.test/a\n"),
            TestContext.Current.CancellationToken);

        playlist.Entries.Should().ContainSingle().Which.Title.Should().Be("News");
    }

    [Fact]
    public async Task Should_StreamEntries_When_ParseEntriesAsyncIsUsed()
    {
        var entries = new List<PlaylistEntry>();
        await foreach (var entry in M3uParser.ParseEntriesAsync(
                           StreamOf(Encoding.UTF8, "#EXTINF:-1,News\nhttp://example.test/a\n"),
                           TestContext.Current.CancellationToken))
        {
            entries.Add(entry);
        }

        entries.Should().ContainSingle().Which.Title.Should().Be("News");
    }

    [Fact]
    public async Task Should_CancelParse_When_TokenIsAlreadyCanceled()
    {
        using var stream = new MemoryStream("#EXTM3U\n"u8.ToArray());

        var act = async () => await M3uParser.ParseAsync(stream, new CancellationToken(canceled: true));

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Should_Throw_When_ContentIsNull()
    {
        var act = () => M3uParser.Parse((string)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Should_Throw_When_EntryUriIsUnusable()
    {
        var act = () => M3uParser.Parse("#EXTINF:-1,Bad\nhttp://[\n");

        act.Should().Throw<PlaylistFormatException>();
    }

    [Fact]
    public void Should_ComparePlaylistsByContent_When_InstancesDiffer()
    {
        const string Text = """
            #EXTM3U url-tvg="http://example.test/epg.xml" refresh="3600"
            #EXTINF:-1 tvg-id="n" tvg-name="News" group-title="News",News
            http://example.test/news
            """;

        var left = M3uParser.Parse(Text);
        var right = M3uParser.Parse(Text);

        ReferenceEquals(left.HeaderAttributes, right.HeaderAttributes).Should().BeFalse();
        ReferenceEquals(left.Entries[0].Attributes, right.Entries[0].Attributes).Should().BeFalse();
        left.Should().Be(right);
        left.GetHashCode().Should().Be(right.GetHashCode());
        left.Entries[0].Should().Be(right.Entries[0]);
        left.Entries[0].GetHashCode().Should().Be(right.Entries[0].GetHashCode());
    }

    [Fact]
    public void Should_CompareEqual_When_AttributeKeysDifferOnlyByCase()
    {
        var lower = M3uParser.Parse("""
            #EXTM3U url-tvg="HTTP://Example.Test/EPG.xml"
            #EXTINF:-1 tvg-id="Channel.One" tvg-name="News",News
            http://example.test/news
            """);
        var upper = M3uParser.Parse("""
            #EXTM3U URL-TVG="HTTP://Example.Test/EPG.xml"
            #EXTINF:-1 TVG-ID="Channel.One" TVG-NAME="News",News
            http://example.test/news
            """);

        lower.Should().Be(upper);
        lower.GetHashCode().Should().Be(upper.GetHashCode());
        upper.HeaderAttributes.Should().ContainKey("url-tvg")
            .WhoseValue.Should().Be("HTTP://Example.Test/EPG.xml");
        var entry = upper.Entries.Should().ContainSingle().Subject;
        entry.Attributes.Should().ContainKey("tvg-id").WhoseValue.Should().Be("Channel.One");
        entry.Attributes.Should().ContainKey("tvg-name").WhoseValue.Should().Be("News");
        entry.TvgId.Should().Be("Channel.One");
        entry.TvgName.Should().Be("News");
    }

    [Fact]
    public void Should_UseCustomKind_When_ParseReceivesAClassifier()
    {
        var playlist = M3uParser.Parse(LiveChannel, new FixedKindClassifier(ContentKind.Movie));

        playlist.Entries.Should().ContainSingle().Which.Kind.Should().Be(ContentKind.Movie);
    }

    [Fact]
    public void Should_UseCustomKind_When_TextReaderParseReceivesAClassifier()
    {
        using var reader = new StringReader(LiveChannel);

        var playlist = M3uParser.Parse(reader, new FixedKindClassifier(ContentKind.Movie));

        playlist.Entries.Should().ContainSingle().Which.Kind.Should().Be(ContentKind.Movie);
    }

    [Fact]
    public async Task Should_UseCustomKind_When_ParseAsyncReceivesAClassifier()
    {
        using var stream = StreamOf(Encoding.UTF8, LiveChannel);

        var playlist = await M3uParser.ParseAsync(
            stream,
            new FixedKindClassifier(ContentKind.Movie),
            TestContext.Current.CancellationToken);

        playlist.Entries.Should().ContainSingle().Which.Kind.Should().Be(ContentKind.Movie);
    }

    [Fact]
    public async Task Should_UseCustomKind_When_ParseEntriesAsyncReceivesAClassifier()
    {
        using var stream = StreamOf(Encoding.UTF8, LiveChannel);
        var entries = new List<PlaylistEntry>();
        await foreach (var entry in M3uParser.ParseEntriesAsync(
                           stream,
                           new FixedKindClassifier(ContentKind.Episode),
                           TestContext.Current.CancellationToken))
        {
            entries.Add(entry);
        }

        entries.Should().ContainSingle().Which.Kind.Should().Be(ContentKind.Episode);
    }

    [Fact]
    public void Should_Throw_When_ClassifierIsNull()
    {
        var act = () => M3uParser.Parse(LiveChannel, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Should_Throw_When_ParseAsyncClassifierIsNull()
    {
        using var stream = StreamOf(Encoding.UTF8, LiveChannel);

        var act = async () => await M3uParser.ParseAsync(stream, null!, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private const string LiveChannel = """
        #EXTM3U
        #EXTINF:-1,News
        http://example.test/live
        """;

    private static MemoryStream StreamOf(Encoding encoding, string text)
    {
        var preamble = encoding.GetPreamble();
        var body = encoding.GetBytes(text);
        var bytes = new byte[preamble.Length + body.Length];
        preamble.CopyTo(bytes, 0);
        body.CopyTo(bytes, preamble.Length);
        return new MemoryStream(bytes);
    }

    private sealed class FixedKindClassifier : IContentKindClassifier
    {
        private readonly ContentKind _kind;

        public FixedKindClassifier(ContentKind kind)
        {
            _kind = kind;
        }

        public ContentKind Classify(PlaylistEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);
            return _kind;
        }
    }
}
