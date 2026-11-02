using System.Text;

namespace UnitTest;

internal static class PlaylistFixtures
{
    public const string NoAttributes = "Fixtures.no-attributes.m3u";
    public const string CommaInTitle = "Fixtures.comma-in-title.m3u";
    public const string Bom = "Fixtures.bom.m3u";
    public const string Crlf = "Fixtures.crlf.m3u";
    public const string LiveM3u8 = "Fixtures.live.m3u8";
    public const string XtreamMovie = "Fixtures.xtream-movie.m3u";
    public const string XtreamSeries = "Fixtures.xtream-series.m3u";
    public const string CorruptPartial = "Fixtures.corrupt-partial.m3u";
    public const string CorruptUri = "Fixtures.corrupt-uri.m3u";
    public const string AttributeKeyCase = "Fixtures.attribute-key-case.m3u";

    public static string ReadText(string name)
    {
        using var stream = Open(name);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    public static byte[] ReadBytes(string name)
    {
        using var stream = Open(name);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    public static Stream Open(string name)
    {
        return typeof(PlaylistFixtures).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Missing embedded fixture '{name}'.");
    }
}
