namespace AmiIptvParser;

/// <summary>Filters and groups <see cref="PlaylistEntry"/> sequences.</summary>
public static class PlaylistEntryExtensions
{
    /// <summary>Entries whose <see cref="PlaylistEntry.Kind"/> is <see cref="ContentKind.Live"/>.</summary>
    /// <param name="entries">Entries to filter.</param>
    /// <returns>Live entries, in the original order.</returns>
    public static IEnumerable<PlaylistEntry> Live(this IEnumerable<PlaylistEntry> entries)
        => WithKind(entries, ContentKind.Live);

    /// <summary>Entries whose <see cref="PlaylistEntry.Kind"/> is <see cref="ContentKind.Movie"/>.</summary>
    /// <param name="entries">Entries to filter.</param>
    /// <returns>Movie entries, in the original order.</returns>
    public static IEnumerable<PlaylistEntry> Movies(this IEnumerable<PlaylistEntry> entries)
        => WithKind(entries, ContentKind.Movie);

    /// <summary>Entries whose <see cref="PlaylistEntry.Kind"/> is <see cref="ContentKind.Episode"/>.</summary>
    /// <param name="entries">Entries to filter.</param>
    /// <returns>Episode entries, in the original order.</returns>
    public static IEnumerable<PlaylistEntry> Episodes(this IEnumerable<PlaylistEntry> entries)
        => WithKind(entries, ContentKind.Episode);

    /// <summary>Distinct group names, in first-seen order. Blank groups are skipped.</summary>
    /// <param name="entries">Entries to read.</param>
    /// <returns>Group names.</returns>
    public static IEnumerable<string> Groups(this IEnumerable<PlaylistEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return DistinctGroups(entries);
    }

    /// <summary>Entries whose <see cref="PlaylistEntry.Group"/> equals <paramref name="group"/>.</summary>
    /// <param name="entries">Entries to filter.</param>
    /// <param name="group">Group name. Comparison is ordinal and case-sensitive.</param>
    /// <returns>Matching entries, in the original order.</returns>
    public static IEnumerable<PlaylistEntry> InGroup(this IEnumerable<PlaylistEntry> entries, string group)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        return entries.Where(entry => string.Equals(entry.Group, group, StringComparison.Ordinal));
    }

    private static IEnumerable<PlaylistEntry> WithKind(IEnumerable<PlaylistEntry> entries, ContentKind kind)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return entries.Where(entry => entry.Kind == kind);
    }

    private static IEnumerable<string> DistinctGroups(IEnumerable<PlaylistEntry> entries)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (entry.Group is { Length: > 0 } group && seen.Add(group))
            {
                yield return group;
            }
        }
    }
}
