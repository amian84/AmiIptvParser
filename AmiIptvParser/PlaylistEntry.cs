using System.Collections.Frozen;

namespace AmiIptvParser;

/// <summary>
/// One channel or title in an M3U playlist.
/// Collection properties are immutable snapshots. Record equality compares those
/// snapshots by reference.
/// </summary>
public sealed record PlaylistEntry
{
    /// <summary>1-based position in the playlist.</summary>
    public required int Number { get; init; }

    /// <summary>Display title. Commas inside the title are preserved.</summary>
    public required string Title { get; init; }

    /// <summary>Stream or media URI. May be relative.</summary>
    public required Uri Url { get; init; }

    /// <summary><c>tvg-id</c> attribute, when present and not blank.</summary>
    public string? TvgId { get; init; }

    /// <summary><c>tvg-name</c> attribute, when present and not blank.</summary>
    public string? TvgName { get; init; }

    /// <summary><c>tvg-logo</c> attribute, when present and not blank.</summary>
    public string? LogoUrl { get; init; }

    /// <summary>
    /// <c>group-title</c> attribute, or the <c>#EXTGRP</c> value that preceded the entry.
    /// </summary>
    public string? Group { get; init; }

    /// <summary>
    /// Declared duration. <see langword="null"/> when the playlist uses a negative duration
    /// (the usual live marker) or omits one.
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Live, movie, or episode. The parser sets this with <see cref="DefaultContentKindClassifier"/>.</summary>
    public ContentKind Kind { get; init; }

    /// <summary>Every <c>key="value"</c> attribute from the <c>#EXTINF</c> line. Never null.</summary>
    public IReadOnlyDictionary<string, string> Attributes { get; init; } =
        FrozenDictionary<string, string>.Empty;
}
