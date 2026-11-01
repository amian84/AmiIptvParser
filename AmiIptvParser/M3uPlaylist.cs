using System.Collections.Immutable;

namespace AmiIptvParser;

/// <summary>
/// An M3U playlist. Properties are get-only.
/// Record equality compares entries and header attributes by content.
/// </summary>
public sealed record M3uPlaylist
{
    /// <summary>Creates a playlist.</summary>
    /// <param name="entries">Entries in file order. Numbers start at 1. A default array is rejected.</param>
    /// <param name="headerAttributes">
    /// Attributes from the <c>#EXTM3U</c> line. Keys produced by the parser are lowercase.
    /// Empty when the header has none.
    /// </param>
    public M3uPlaylist(
        ImmutableArray<PlaylistEntry> entries,
        ImmutableDictionary<string, string> headerAttributes)
    {
        if (entries.IsDefault)
        {
            throw new ArgumentException("Entries must be initialized.", nameof(entries));
        }

        ArgumentNullException.ThrowIfNull(headerAttributes);
        Entries = entries;
        HeaderAttributes = headerAttributes;
    }

    /// <summary>Entries in file order. Numbers start at 1.</summary>
    public ImmutableArray<PlaylistEntry> Entries { get; }

    /// <summary>
    /// Attributes from the <c>#EXTM3U</c> line. Keys produced by the parser are lowercase.
    /// Empty when the header has none.
    /// </summary>
    public ImmutableDictionary<string, string> HeaderAttributes { get; }

    /// <summary>Compares playlists by value, including entry and header content.</summary>
    /// <param name="other">Playlist to compare.</param>
    /// <returns>True when the entries and header attributes match.</returns>
    public bool Equals(M3uPlaylist? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return ContentEquality.EntriesEqual(Entries, other.Entries)
               && ContentEquality.DictionaryEquals(HeaderAttributes, other.HeaderAttributes);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContentEquality.EntriesHash(Entries));
        hash.Add(ContentEquality.DictionaryHash(HeaderAttributes));
        return hash.ToHashCode();
    }
}
