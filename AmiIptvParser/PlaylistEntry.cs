using System.Collections.Immutable;

namespace AmiIptvParser;

/// <summary>
/// One channel or title in an M3U playlist.
/// Properties are get-only. Record equality compares attributes by content.
/// </summary>
public sealed record PlaylistEntry
{
    /// <summary>Creates an entry.</summary>
    /// <param name="number">1-based position in the playlist.</param>
    /// <param name="title">Display title. Commas inside the title are preserved.</param>
    /// <param name="url">Stream or media URI. May be relative.</param>
    /// <param name="tvgId"><c>tvg-id</c> attribute, when present and not blank.</param>
    /// <param name="tvgName"><c>tvg-name</c> attribute, when present and not blank.</param>
    /// <param name="logoUrl"><c>tvg-logo</c> attribute, when present and not blank.</param>
    /// <param name="group"><c>group-title</c> or the preceding <c>#EXTGRP</c> value.</param>
    /// <param name="duration">Declared duration. Null when the playlist uses a negative duration or omits one.</param>
    /// <param name="kind">Live, movie, or episode.</param>
    /// <param name="attributes">
    /// Every <c>key="value"</c> attribute from the <c>#EXTINF</c> line.
    /// Null is stored as an empty dictionary.
    /// </param>
    public PlaylistEntry(
        int number,
        string title,
        Uri url,
        string? tvgId = null,
        string? tvgName = null,
        string? logoUrl = null,
        string? group = null,
        TimeSpan? duration = null,
        ContentKind kind = ContentKind.Live,
        ImmutableDictionary<string, string>? attributes = null)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(url);
        Number = number;
        Title = title;
        Url = url;
        TvgId = tvgId;
        TvgName = tvgName;
        LogoUrl = logoUrl;
        Group = group;
        Duration = duration;
        Kind = kind;
        Attributes = attributes ?? ImmutableDictionary<string, string>.Empty;
    }

    /// <summary>1-based position in the playlist.</summary>
    public int Number { get; }

    /// <summary>Display title. Commas inside the title are preserved.</summary>
    public string Title { get; }

    /// <summary>Stream or media URI. May be relative.</summary>
    public Uri Url { get; }

    /// <summary><c>tvg-id</c> attribute, when present and not blank.</summary>
    public string? TvgId { get; }

    /// <summary><c>tvg-name</c> attribute, when present and not blank.</summary>
    public string? TvgName { get; }

    /// <summary><c>tvg-logo</c> attribute, when present and not blank.</summary>
    public string? LogoUrl { get; }

    /// <summary>
    /// <c>group-title</c> attribute, or the <c>#EXTGRP</c> value that preceded the entry.
    /// </summary>
    public string? Group { get; }

    /// <summary>
    /// Declared duration. <see langword="null"/> when the playlist uses a negative duration
    /// (the usual live marker) or omits one.
    /// </summary>
    public TimeSpan? Duration { get; }

    /// <summary>Live, movie, or episode. The parser sets this with the active <see cref="IContentKindClassifier"/>.</summary>
    public ContentKind Kind { get; }

    /// <summary>Every <c>key="value"</c> attribute from the <c>#EXTINF</c> line. Never null.</summary>
    public ImmutableDictionary<string, string> Attributes { get; }

    /// <summary>Compares entries by value, including attribute content.</summary>
    /// <param name="other">Entry to compare.</param>
    /// <returns>True when every property matches.</returns>
    public bool Equals(PlaylistEntry? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Number == other.Number
               && string.Equals(Title, other.Title, StringComparison.Ordinal)
               && Url == other.Url
               && string.Equals(TvgId, other.TvgId, StringComparison.Ordinal)
               && string.Equals(TvgName, other.TvgName, StringComparison.Ordinal)
               && string.Equals(LogoUrl, other.LogoUrl, StringComparison.Ordinal)
               && string.Equals(Group, other.Group, StringComparison.Ordinal)
               && Duration == other.Duration
               && Kind == other.Kind
               && ContentEquality.DictionaryEquals(Attributes, other.Attributes);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Number);
        hash.Add(Title, StringComparer.Ordinal);
        hash.Add(Url);
        hash.Add(TvgId, StringComparer.Ordinal);
        hash.Add(TvgName, StringComparer.Ordinal);
        hash.Add(LogoUrl, StringComparer.Ordinal);
        hash.Add(Group, StringComparer.Ordinal);
        hash.Add(Duration);
        hash.Add(Kind);
        hash.Add(ContentEquality.DictionaryHash(Attributes));
        return hash.ToHashCode();
    }

    internal PlaylistEntry WithKind(ContentKind kind)
    {
        if (kind == Kind)
        {
            return this;
        }

        return new PlaylistEntry(
            Number,
            Title,
            Url,
            TvgId,
            TvgName,
            LogoUrl,
            Group,
            Duration,
            kind,
            Attributes);
    }
}
