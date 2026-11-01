using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Text;

namespace AmiIptvParser;

/// <summary>
/// Parses M3U and M3U8 text. Entries without attributes are kept. The title is the text after
/// the first comma that sits outside quotes. A leading byte-order mark is accepted.
/// An <c>#EXTINF</c> that never gets a URI is dropped. Classification uses
/// <see cref="DefaultContentKindClassifier"/>.
/// </summary>
public static class M3uParser
{
    private const int ReaderBufferSize = 4096;

    /// <summary>Parses <paramref name="content"/>.</summary>
    /// <param name="content">Playlist text. A leading U+FEFF is ignored.</param>
    /// <returns>The playlist. Empty when <paramref name="content"/> has no entries.</returns>
    public static M3uPlaylist Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Length > 0 && content[0] == '\uFEFF')
        {
            content = content[1..];
        }

        using var reader = new StringReader(content);
        return Parse(reader);
    }

    /// <summary>Parses lines from <paramref name="reader"/>.</summary>
    /// <param name="reader">Line source. The caller owns and disposes it.</param>
    /// <returns>The playlist.</returns>
    public static M3uPlaylist Parse(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var state = new M3uParseState();
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

    /// <summary>Parses <paramref name="stream"/> as text, detecting a BOM.</summary>
    /// <param name="stream">Playlist bytes. This method does not dispose it.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The playlist.</returns>
    public static async Task<M3uPlaylist> ParseAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyDictionary<string, string>? header = null;
        var entries = new List<PlaylistEntry>();
        await foreach (var entry in ReadEntries(stream, value => header = value, cancellationToken)
                           .ConfigureAwait(false))
        {
            entries.Add(entry);
        }

        return CreatePlaylist(entries, header ?? FrozenDictionary<string, string>.Empty);
    }

    /// <summary>
    /// Streams entries from <paramref name="stream"/>. Header attributes are not returned;
    /// use <see cref="ParseAsync(Stream, CancellationToken)"/> when the header is needed.
    /// </summary>
    /// <param name="stream">Playlist bytes. This method does not dispose it.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>Entries in file order.</returns>
    public static IAsyncEnumerable<PlaylistEntry> ParseEntriesAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return ReadEntries(stream, headerSink: null, cancellationToken);
    }

    private static M3uPlaylist CreatePlaylist(
        List<PlaylistEntry> entries,
        IReadOnlyDictionary<string, string> header)
    {
        IReadOnlyList<PlaylistEntry> published = entries.Count == 0 ? [] : entries.ToArray();
        return new M3uPlaylist(published, header);
    }

    private static async IAsyncEnumerable<PlaylistEntry> ReadEntries(
        Stream stream,
        Action<IReadOnlyDictionary<string, string>>? headerSink,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: ReaderBufferSize,
            leaveOpen: true);
        var state = new M3uParseState();
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
