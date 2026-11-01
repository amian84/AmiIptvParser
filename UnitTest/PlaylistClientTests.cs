using System.Net;
using System.Text;

using AmiIptvParser;

using AwesomeAssertions;

using Microsoft.Extensions.Logging;

using Xunit;

namespace UnitTest;

public sealed class PlaylistClientTests
{
    private const string PlaylistBody = "#EXTM3U\n#EXTINF:-1,News\nhttp://example.test/live\n";

    [Fact]
    public async Task Should_ParseBody_When_ResponseIsSuccessful()
    {
        using var provider = TestClients.CreateProvider(new RecordingHandler(HttpStatusCode.OK, PlaylistBody));
        var client = new PlaylistClient(TestClients.CreateHttpClient(provider));

        var playlist = await client.LoadAsync(
            new Uri("http://example.test/list.m3u"),
            TestContext.Current.CancellationToken);

        playlist.Entries.Should().ContainSingle().Which.Title.Should().Be("News");
    }

    [Fact]
    public async Task Should_SendUserAgent_When_OptionsSetIt()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, PlaylistBody);
        using var provider = TestClients.CreateProvider(handler);
        var options = new PlaylistClientOptions("CustomPlayer/2");
        var client = new PlaylistClient(TestClients.CreateHttpClient(provider), options);

        await client.LoadAsync(
            new Uri("http://example.test/list.m3u"),
            TestContext.Current.CancellationToken);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest.Headers.TryGetValues("User-Agent", out var values).Should().BeTrue();
        values.Should().ContainSingle().Which.Should().Be("CustomPlayer/2");
    }

    [Fact]
    public async Task Should_Throw_When_ResponseStatusIsNotSuccess()
    {
        using var provider = TestClients.CreateProvider(
            new RecordingHandler(HttpStatusCode.NotFound, "<html></html>"));
        var client = new PlaylistClient(TestClients.CreateHttpClient(provider));

        var act = async () => await client.LoadAsync(
            new Uri("http://example.test/missing.m3u"),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task Should_OmitCredentialsFromLogs_When_UrlContainsAPassword()
    {
        var logger = new ListLogger<PlaylistClient>();
        using var provider = TestClients.CreateProvider(new RecordingHandler(HttpStatusCode.OK, PlaylistBody));
        var client = new PlaylistClient(TestClients.CreateHttpClient(provider), logger: logger);

        await client.LoadAsync(
            new Uri("http://user:secret-pass@example.test/get.php?password=secret-pass"),
            TestContext.Current.CancellationToken);

        var written = string.Join('\n', logger.Messages);
        written.Should().NotContain("secret-pass");
        written.Should().Contain("example.test");
    }

    [Fact]
    public async Task Should_CancelLoad_When_TokenIsAlreadyCanceled()
    {
        using var provider = TestClients.CreateProvider(new RecordingHandler(HttpStatusCode.OK, PlaylistBody));
        var client = new PlaylistClient(TestClients.CreateHttpClient(provider));

        var act = async () => await client.LoadAsync(
            new Uri("http://example.test/list.m3u"),
            new CancellationToken(canceled: true));

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Should_Throw_When_UserAgentContainsALineBreak()
    {
        using var provider = TestClients.CreateProvider(new RecordingHandler(HttpStatusCode.OK, PlaylistBody));

        var act = () => new PlaylistClient(
            TestClients.CreateHttpClient(provider),
            new PlaylistClientOptions("bad\r\nX"));

        act.Should().Throw<ArgumentException>();
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public RecordingHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastRequest = request;
            var response = new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "audio/mpegurl"),
            };
            return Task.FromResult(response);
        }
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
