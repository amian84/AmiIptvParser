namespace AmiIptvParser;

/// <summary>
/// An M3U playlist. Collection properties are immutable snapshots.
/// </summary>
public sealed record M3uPlaylist
{
    /// <summary>Creates a playlist.</summary>
    /// <param name="entries">Entries in file order. Numbers start at 1.</param>
    /// <param name="headerAttributes">Attributes from the <c>#EXTM3U</c> line. Empty when the header has none.</param>
    public M3uPlaylist(
        IReadOnlyList<PlaylistEntry> entries,
        IReadOnlyDictionary<string, string> headerAttributes)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(headerAttributes);
        Entries = entries;
        HeaderAttributes = headerAttributes;
    }

    /// <summary>Entries in file order. Numbers start at 1.</summary>
    public IReadOnlyList<PlaylistEntry> Entries { get; init; }

    /// <summary>Attributes from the <c>#EXTM3U</c> line. Empty when the header has none.</summary>
    public IReadOnlyDictionary<string, string> HeaderAttributes { get; init; }
}
