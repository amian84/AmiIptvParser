namespace AmiIptvParser;

/// <summary>
/// The playlist text is not usable M3U. Incomplete trailing <c>#EXTINF</c> lines are ignored;
/// this exception is reserved for an entry whose URI cannot be parsed, or a duration that does
/// not fit in <see cref="TimeSpan"/>.
/// </summary>
public sealed class PlaylistFormatException : Exception
{
    /// <summary>Creates an exception with a default message.</summary>
    public PlaylistFormatException()
        : base("The playlist is not valid M3U.")
    {
    }

    /// <summary>Creates an exception with <paramref name="message"/>.</summary>
    /// <param name="message">What was wrong with the playlist.</param>
    public PlaylistFormatException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with <paramref name="message"/> and <paramref name="innerException"/>.</summary>
    /// <param name="message">What was wrong with the playlist.</param>
    /// <param name="innerException">The error that caused this exception.</param>
    public PlaylistFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
