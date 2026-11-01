namespace AmiIptvParser;

/// <summary>Settings for <see cref="PlaylistClient"/>.</summary>
public sealed record PlaylistClientOptions
{
    /// <summary>Creates options that send <paramref name="userAgent"/>.</summary>
    /// <param name="userAgent">User-Agent header value. Must not be null. The client rejects blank values and line breaks.</param>
    public PlaylistClientOptions(string userAgent)
    {
        ArgumentNullException.ThrowIfNull(userAgent);
        UserAgent = userAgent;
    }

    /// <summary>
    /// User-Agent sent on each load. Defaults to <see cref="PlaylistClient.DefaultUserAgent"/>.
    /// </summary>
    public string UserAgent { get; }

    /// <summary>Options that send <see cref="PlaylistClient.DefaultUserAgent"/>.</summary>
    public static PlaylistClientOptions Default { get; } = new(PlaylistClient.DefaultUserAgent);
}
