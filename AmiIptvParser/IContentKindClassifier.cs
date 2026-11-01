namespace AmiIptvParser;

/// <summary>
/// Decides whether an entry is live, a movie, or an episode.
/// The parser uses <see cref="DefaultContentKindClassifier"/>.
/// </summary>
public interface IContentKindClassifier
{
    /// <summary>Classifies <paramref name="entry"/> from its URL and title.</summary>
    /// <param name="entry">Entry whose <see cref="PlaylistEntry.Kind"/> may be ignored.</param>
    /// <returns>The kind that should be stored on the entry.</returns>
    ContentKind Classify(PlaylistEntry entry);
}
