using System.Text;
using System.Text.Json;
using AmiIptvParser;
using AwesomeAssertions;
using NUnit.Framework;

namespace UnitTest;

public sealed class AmiParserTests
{
    // run.mocky.io no longer serves this playlist (TLS / connection failure).
    // ProcessListSync/Async are the only callers that download it. Initializer does not.
    // Ignored until a later phase replaces them with an embedded M3U fixture.
    private const string MockyPlaylistUrl =
        "https://run.mocky.io/v3/e68af937-3b8f-4c8a-91d6-ebe27ed1c17b";

    private const string DeadMockyFixture =
        "Ignored: downloads " + MockyPlaylistUrl
        + ", which no longer serves the M3U fixture (TLS/connection failure). "
        + "Kept until an in-memory playlist replaces the network call. Parser code is unchanged.";

    [SetUp]
    public void Setup()
    {
        var testListUrl = new UrlObject
        {
            Url = MockyPlaylistUrl,
            Name = "Test"
        };

        AmiParser.Initializer(testListUrl, loadCache: false);
    }

    [Test]
    [Ignore(DeadMockyFixture)]
    public void Should_ReturnChannels_When_PlaylistIsProcessed()
    {
        AmiParser.ProcessListSync();

        AmiParser.GetAllChannels().Should().NotBeEmpty();
    }

    [Test]
    [Ignore(DeadMockyFixture)]
    public void Should_ReturnCanal4_When_ChannelNumberIs3()
    {
        AmiParser.ProcessListSync();

        AmiParser.GetChannelNumber(3).Title.Should().Be("Canal4");
    }

    [Test]
    [Ignore(DeadMockyFixture)]
    public void Should_ReturnFourGroups_When_PlaylistIsProcessed()
    {
        AmiParser.ProcessListSync();

        AmiParser.GetAllGroups().Should().HaveCount(4);
    }

    [Test]
    [Ignore(DeadMockyFixture)]
    public void Should_ReturnTwoShowGroups_When_PlaylistIsProcessed()
    {
        AmiParser.ProcessListSync();

        AmiParser.GetAllShowsGroups().Should().HaveCount(2);
    }

    [Test]
    public void Should_LoadGroupsAndOneMovie_When_CacheStreamIsRead()
    {
        var channels = new List<ChannelInfo>
        {
            new(1, "Channel1", "Group1", "", "", ChType.Channel),
            new(2, "Channel2", "Group1", "", "", ChType.Channel),
            new(3, "Channel3", "Group2", "", "", ChType.Channel),
            new(4, "Channel4", "Group2", "", "", ChType.Channel),
            new(5, "Movies", "MOVIES", "", "", ChType.Movie)
        };

        using var reader = BuildCacheReader(channels);

        AmiParserToTest.CheckCacheTest(reader);

        AmiParser.GetAllGroups().Should().HaveCount(3);
        AmiParser.GetMovies().Should().ContainSingle();
    }

    [Test]
    public void Should_ReturnSeenMovie_When_CacheContainsSeenInfo()
    {
        var channels = new List<ChannelInfo>
        {
            new(1, "Channel1", "Group1", "", "", ChType.Channel),
            new(2, "Channel2", "Group1", "", "", ChType.Channel),
            new(3, "Channel3", "Group2", "", "", ChType.Channel),
            new(4, "Mov1", "MOVIES", "", "", ChType.Movie),
            new(5, "Mov2", "MOVIES", "", "", ChType.Movie)
        };
        channels[4].SeenInfoCh = new SeenInfo
        {
            Seen = true,
            HistoryDate = new DateTime(2022, 1, 1)
        };

        using var reader = BuildCacheReader(channels);

        AmiParserToTest.CheckCacheTest(reader);

        var seen = AmiParser.GetHistorySeen().Should().ContainSingle().Subject;
        seen.Seen.Should().BeTrue();
        seen.Date.Should().Be(new DateTime(2022, 1, 1));
        seen.Title.Should().Be("Mov2");
    }

    [Test]
    [Ignore(DeadMockyFixture)]
    public void Should_SignalStartAndEnd_When_ProcessListAsyncRuns()
    {
        var are = new AutoResetEvent(false);
        AmiParser.SubscribeStartEventProcessList(() =>
        {
            AmiParser.GetStatus().Should().Be(ChStatus.Initializing);
        });

        AmiParser.SubscribeEndEventProcessList(status =>
        {
            status.Should().Be(ChStatus.Initialize);
            are.Set();
        });
        AmiParser.ProcessListAsync();
        var wasSignaled = are.WaitOne(timeout: TimeSpan.FromSeconds(5));

        wasSignaled.Should().BeTrue();
    }

    private static StreamReader BuildCacheReader(IReadOnlyList<ChannelInfo> channels)
    {
        var json = JsonSerializer.Serialize(channels);
        var stream = new MemoryStream(Encoding.ASCII.GetBytes(json));
        return new StreamReader(stream);
    }
}
