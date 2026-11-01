using Microsoft.Extensions.Logging;

namespace AmiIptvParser;

/// <summary>
/// Downloads an M3U playlist with a caller-owned <see cref="HttpClient"/> and parses it.
/// The caller disposes the client. This type does not cache playlists on disk.
/// </summary>
public sealed partial class PlaylistClient
{
    /// <summary>User-Agent used when <see cref="PlaylistClientOptions.UserAgent"/> is left at its default.</summary>
    public const string DefaultUserAgent = "AmiIptvParser/1.0";

    private readonly HttpClient _httpClient;
    private readonly ILogger<PlaylistClient>? _logger;
    private readonly string _userAgent;

    /// <summary>Creates a client.</summary>
    /// <param name="httpClient">Caller-owned client. Timeouts and handlers belong to it.</param>
    /// <param name="options">
    /// Request settings. <see langword="null"/> selects <see cref="PlaylistClientOptions.Default"/>.
    /// </param>
    /// <param name="logger">Optional logger. The playlist URI is not written to logs.</param>
    public PlaylistClient(
        HttpClient httpClient,
        PlaylistClientOptions? options = null,
        ILogger<PlaylistClient>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        var settings = options ?? PlaylistClientOptions.Default;
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.UserAgent);
        if (settings.UserAgent.AsSpan().ContainsAny('\r', '\n'))
        {
            throw new ArgumentException("User-Agent cannot contain line breaks.", nameof(options));
        }

        _httpClient = httpClient;
        _logger = logger;
        _userAgent = settings.UserAgent;
    }

    /// <summary>
    /// Downloads <paramref name="url"/> and parses the body as M3U.
    /// A non-success status throws <see cref="HttpRequestException"/> and the body is not parsed.
    /// </summary>
    /// <param name="url">Absolute playlist URI.</param>
    /// <param name="cancellationToken">Cancels the download and the parse.</param>
    /// <returns>The parsed playlist.</returns>
    public async Task<M3uPlaylist> LoadAsync(Uri url, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(url);
        if (!url.IsAbsoluteUri)
        {
            throw new ArgumentException("Playlist URL must be absolute.", nameof(url));
        }

        // Xtream URLs embed credentials in the user-info or the path. Log the host only.
        if (_logger is { } logger && logger.IsEnabled(LogLevel.Debug))
        {
            LogLoading(logger, url.Host);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);
        request.Headers.TryAddWithoutValidation("Accept", "*/*");

        using var response = await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            if (_logger is { } failureLogger && failureLogger.IsEnabled(LogLevel.Warning))
            {
                LogRejected(failureLogger, url.Host, (int)response.StatusCode);
            }

            response.EnsureSuccessStatusCode();
        }

        await using var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        var playlist = await M3uParser.ParseAsync(stream, cancellationToken).ConfigureAwait(false);
        if (_logger is { } loadedLogger && loadedLogger.IsEnabled(LogLevel.Debug))
        {
            LogLoaded(loadedLogger, playlist.Entries.Count);
        }

        return playlist;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Loading playlist from {Host}")]
    private static partial void LogLoading(ILogger logger, string host);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Playlist request to {Host} failed with status {StatusCode}")]
    private static partial void LogRejected(ILogger logger, string host, int statusCode);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Loaded playlist with {EntryCount} entries")]
    private static partial void LogLoaded(ILogger logger, int entryCount);
}
