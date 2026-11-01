namespace AmiIptvParser;

/// <summary>Settings for <see cref="PlaylistClient"/>.</summary>
public sealed record PlaylistClientOptions
{
    /// <summary>
    /// User-Agent sent on each load. Defaults to <see cref="PlaylistClient.DefaultUserAgent"/>.
    /// </summary>
    public string UserAgent { get; init; } = PlaylistClient.DefaultUserAgent;

    /// <summary>Options that send <see cref="PlaylistClient.DefaultUserAgent"/>.</summary>
    public static PlaylistClientOptions Default { get; } = new();
}
