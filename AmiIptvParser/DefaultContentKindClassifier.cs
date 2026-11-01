using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace AmiIptvParser;

/// <summary>
/// Classifies Xtream <c>/movie/</c> and <c>/series/</c> paths, then the file extension
/// with the query string removed. An <c>SxxEyy</c> title marks an episode only when the
/// file extension is a video container. An HLS <c>.m3u8</c> manifest is live.
/// </summary>
public sealed partial class DefaultContentKindClassifier : IContentKindClassifier
{
    private static readonly FrozenSet<string> MovieExtensions = FrozenSet.ToFrozenSet(
        [".mkv", ".avi", ".mp4", ".m4v", ".mov", ".wmv", ".flv", ".webm", ".mpg", ".mpeg"],
        StringComparer.OrdinalIgnoreCase);

    /// <summary>Classifies <paramref name="entry"/>.</summary>
    /// <param name="entry">Entry to classify. Its current <see cref="PlaylistEntry.Kind"/> is ignored.</param>
    /// <returns>Live, movie, or episode.</returns>
    public ContentKind Classify(PlaylistEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var path = PathWithoutQuery(entry.Url);
        if (HasSegment(path, "series"))
        {
            return ContentKind.Episode;
        }

        if (HasSegment(path, "movie"))
        {
            return ContentKind.Movie;
        }

        var extension = Path.GetExtension(path);
        if (extension.Equals(".m3u8", StringComparison.OrdinalIgnoreCase))
        {
            return ContentKind.Live;
        }

        if (!MovieExtensions.Contains(extension))
        {
            return ContentKind.Live;
        }

        if (EpisodeTitle().IsMatch(entry.Title))
        {
            return ContentKind.Episode;
        }

        return ContentKind.Movie;
    }

    /// <summary>Matches <c>SxxEyy</c> as its own token, ignoring case.</summary>
    [GeneratedRegex(
        """(?<![A-Za-z0-9])S\d{1,3}\s*E\d{1,3}(?![A-Za-z0-9])""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture)]
    private static partial Regex EpisodeTitle();

    private static string PathWithoutQuery(Uri url)
    {
        if (url.IsAbsoluteUri)
        {
            return url.AbsolutePath;
        }

        var text = url.OriginalString;
        var cut = text.IndexOfAny(['?', '#']);
        return cut < 0 ? text : text[..cut];
    }

    private static bool HasSegment(string path, string segment)
    {
        var start = 0;
        while (start <= path.Length)
        {
            var slash = path.IndexOf('/', start);
            var end = slash < 0 ? path.Length : slash;
            if (IsSegment(path, start, end, segment))
            {
                return true;
            }

            if (slash < 0)
            {
                return false;
            }

            start = slash + 1;
        }

        return false;
    }

    private static bool IsSegment(string path, int start, int end, string segment)
        => end > start
           && path.AsSpan(start, end - start).Equals(segment, StringComparison.OrdinalIgnoreCase);
}
