using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace AmiIptvParser;

/// <summary>
/// Parses M3U and M3U8 text. Entries without attributes are kept. The title is the text after
/// the first comma that sits outside quotes. A leading byte-order mark is accepted.
/// An <c>#EXTINF</c> that never gets a URI is dropped. Overloads without a classifier use
/// <see cref="DefaultContentKindClassifier"/>.
/// </summary>
public static class M3uParser
{
    private const int ReaderBufferSize = 4096;

    private static readonly IContentKindClassifier DefaultClassifier = new DefaultContentKindClassifier();

    /// <summary>Parses <paramref name="content"/> with <see cref="DefaultContentKindClassifier"/>.</summary>
    /// <param name="content">Playlist text. A leading U+FEFF is ignored.</param>
    /// <returns>The playlist. Empty when <paramref name="content"/> has no entries.</returns>
    public static M3uPlaylist Parse(string content)
        => Parse(content, DefaultClassifier);

    /// <summary>Parses <paramref name="content"/> and classifies each entry with <paramref name="classifier"/>.</summary>
    /// <param name="content">Playlist text. A leading U+FEFF is ignored.</param>
    /// <param name="classifier">Assigns <see cref="PlaylistEntry.Kind"/>. Required.</param>
    /// <returns>The playlist. Empty when <paramref name="content"/> has no entries.</returns>
    public static M3uPlaylist Parse(string content, IContentKindClassifier classifier)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(classifier);
        if (content.Length > 0 && content[0] == '\uFEFF')
        {
            content = content[1..];
        }

        using var reader = new StringReader(content);
        return Parse(reader, classifier);
    }

    /// <summary>Parses lines from <paramref name="reader"/> with <see cref="DefaultContentKindClassifier"/>.</summary>
    /// <param name="reader">Line source. The caller owns and disposes it.</param>
    /// <returns>The playlist.</returns>
    public static M3uPlaylist Parse(TextReader reader)
        => Parse(reader, DefaultClassifier);

    /// <summary>Parses lines from <paramref name="reader"/> and classifies each entry with <paramref name="classifier"/>.</summary>
    /// <param name="reader">Line source. The caller owns and disposes it.</param>
    /// <param name="classifier">Assigns <see cref="PlaylistEntry.Kind"/>. Required.</param>
    /// <returns>The playlist.</returns>
    public static M3uPlaylist Parse(TextReader reader, IContentKindClassifier classifier)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(classifier);
        var state = new M3uParseState(classifier);
        var entries = new List<PlaylistEntry>();
        while (reader.ReadLine() is { } line)
        {
            if (state.Consume(line) is { } entry)
            {
                entries.Add(entry);
            }
        }

        return CreatePlaylist(entries, state.HeaderAttributes);
    }

    /// <summary>
    /// Parses <paramref name="stream"/> as text, detecting a BOM, with <see cref="DefaultContentKindClassifier"/>.
    /// </summary>
    /// <param name="stream">Playlist bytes. This method does not dispose it.</param>
    /// <returns>The playlist.</returns>
    public static Task<M3uPlaylist> ParseAsync(Stream stream)
        => ParseAsync(stream, DefaultClassifier, CancellationToken.None);

    /// <summary>
    /// Parses <paramref name="stream"/> as text, detecting a BOM, with <see cref="DefaultContentKindClassifier"/>.
    /// </summary>
    /// <param name="stream">Playlist bytes. This method does not dispose it.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The playlist.</returns>
    public static Task<M3uPlaylist> ParseAsync(Stream stream, CancellationToken cancellationToken)
        => ParseAsync(stream, DefaultClassifier, cancellationToken);

    /// <summary>Parses <paramref name="stream"/> as text and classifies each entry with <paramref name="classifier"/>.</summary>
    /// <param name="stream">Playlist bytes. This method does not dispose it.</param>
    /// <param name="classifier">Assigns <see cref="PlaylistEntry.Kind"/>. Required.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The playlist.</returns>
    public static async Task<M3uPlaylist> ParseAsync(
        Stream stream,
        IContentKindClassifier classifier,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(classifier);
        cancellationToken.ThrowIfCancellationRequested();

        ImmutableDictionary<string, string>? header = null;
        var entries = new List<PlaylistEntry>();
        await foreach (var entry in ReadEntries(stream, classifier, value => header = value, cancellationToken)
                           .ConfigureAwait(false))
        {
            entries.Add(entry);
        }

        return CreatePlaylist(entries, header ?? ImmutableDictionary<string, string>.Empty);
    }

    /// <summary>
    /// Streams entries from <paramref name="stream"/> with <see cref="DefaultContentKindClassifier"/>.
    /// Header attributes are not returned; use <see cref="ParseAsync(Stream, CancellationToken)"/> when the header is needed.
    /// </summary>
    /// <param name="stream">Playlist bytes. This method does not dispose it.</param>
    /// <returns>Entries in file order.</returns>
    public static IAsyncEnumerable<PlaylistEntry> ParseEntriesAsync(Stream stream)
        => ParseEntriesAsync(stream, DefaultClassifier, CancellationToken.None);

    /// <summary>
    /// Streams entries from <paramref name="stream"/> with <see cref="DefaultContentKindClassifier"/>.
    /// Header attributes are not returned; use <see cref="ParseAsync(Stream, CancellationToken)"/> when the header is needed.
    /// </summary>
    /// <param name="stream">Playlist bytes. This method does not dispose it.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>Entries in file order.</returns>
    public static IAsyncEnumerable<PlaylistEntry> ParseEntriesAsync(
        Stream stream,
        CancellationToken cancellationToken)
        => ParseEntriesAsync(stream, DefaultClassifier, cancellationToken);

    /// <summary>
    /// Streams entries from <paramref name="stream"/> and classifies each one with <paramref name="classifier"/>.
    /// Header attributes are not returned.
    /// </summary>
    /// <param name="stream">Playlist bytes. This method does not dispose it.</param>
    /// <param name="classifier">Assigns <see cref="PlaylistEntry.Kind"/>. Required.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>Entries in file order.</returns>
    public static IAsyncEnumerable<PlaylistEntry> ParseEntriesAsync(
        Stream stream,
        IContentKindClassifier classifier,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(classifier);
        return ReadEntries(stream, classifier, headerSink: null, cancellationToken);
    }

    private static M3uPlaylist CreatePlaylist(
        List<PlaylistEntry> entries,
        ImmutableDictionary<string, string> header)
    {
        var published = entries.Count == 0
            ? ImmutableArray<PlaylistEntry>.Empty
            : entries.ToImmutableArray();
        return new M3uPlaylist(published, header);
    }

    private static async IAsyncEnumerable<PlaylistEntry> ReadEntries(
        Stream stream,
        IContentKindClassifier classifier,
        Action<ImmutableDictionary<string, string>>? headerSink,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: ReaderBufferSize,
            leaveOpen: true);
        var state = new M3uParseState(classifier);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            if (state.Consume(line) is { } entry)
            {
                yield return entry;
            }
        }

        headerSink?.Invoke(state.HeaderAttributes);
    }
}
