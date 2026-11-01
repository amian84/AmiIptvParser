namespace AmiIptvParser;

/// <summary>
/// What a playlist entry points at.
/// </summary>
public enum ContentKind
{
    /// <summary>A live channel, including an HLS <c>.m3u8</c> manifest.</summary>
    Live = 0,

    /// <summary>A video-on-demand movie.</summary>
    Movie = 1,

    /// <summary>An episode of a series.</summary>
    Episode = 2,
}
