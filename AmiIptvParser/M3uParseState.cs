using System.Collections.Frozen;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AmiIptvParser;

internal sealed partial class M3uParseState
{
    private static readonly DefaultContentKindClassifier Classifier = new();

    private readonly Dictionary<string, string> _header = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string>? _pendingAttributes;
    private string _pendingTitle = string.Empty;
    private string? _extGrp;
    private TimeSpan? _pendingDuration;
    private bool _headerRead;
    private bool _hasPending;
    private int _nextNumber = 1;

    public FrozenDictionary<string, string> HeaderAttributes =>
        _header.Count == 0
            ? FrozenDictionary<string, string>.Empty
            : _header.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public PlaylistEntry? Consume(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed[0] == '#')
        {
            ConsumeTag(trimmed);
            return null;
        }

        return ConsumeUrl(trimmed);
    }

    private void ConsumeTag(string line)
    {
        if (!_headerRead && IsExtM3u(line, out var headerRest))
        {
            _headerRead = true;
            ReadAttributes(headerRest, _header);
            return;
        }

        if (TryMatchPrefix(line, "#EXTINF:", out var extInfRest))
        {
            BeginEntry(extInfRest);
            return;
        }

        if (TryMatchPrefix(line, "#EXTGRP:", out var groupRest))
        {
            _extGrp = NullIfBlank(groupRest.Trim());
        }
    }

    private void BeginEntry(string rest)
    {
        _hasPending = true;
        _pendingAttributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var span = rest.AsSpan().TrimStart();
        var consumed = ReadDuration(span, out _pendingDuration);
        var afterDuration = span[consumed..].TrimStart();
        var comma = IndexOfCommaOutsideQuotes(afterDuration);
        if (comma < 0)
        {
            _pendingTitle = string.Empty;
            ReadAttributes(afterDuration.ToString(), _pendingAttributes);
            return;
        }

        ReadAttributes(afterDuration[..comma].ToString(), _pendingAttributes);
        _pendingTitle = afterDuration[(comma + 1)..].Trim().ToString();
    }

    private PlaylistEntry? ConsumeUrl(string urlText)
    {
        if (!_hasPending)
        {
            return null;
        }

        if (!Uri.TryCreate(urlText, UriKind.RelativeOrAbsolute, out var url))
        {
            throw new PlaylistFormatException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Playlist entry {_nextNumber} does not contain a usable URI."));
        }

        var attributes = Publish(_pendingAttributes);
        var draft = new PlaylistEntry
        {
            Number = _nextNumber,
            Title = _pendingTitle,
            Url = url,
            TvgId = Optional(attributes, "tvg-id"),
            TvgName = Optional(attributes, "tvg-name"),
            LogoUrl = Optional(attributes, "tvg-logo"),
            Group = Optional(attributes, "group-title") ?? NullIfBlank(_extGrp),
            Duration = _pendingDuration,
            Kind = ContentKind.Live,
            Attributes = attributes,
        };

        ClearPending();
        _nextNumber++;
        return draft with { Kind = Classifier.Classify(draft) };
    }

    private void ClearPending()
    {
        _hasPending = false;
        _pendingTitle = string.Empty;
        _pendingDuration = null;
        _pendingAttributes = null;
        _extGrp = null;
    }

    private static int ReadDuration(ReadOnlySpan<char> text, out TimeSpan? duration)
    {
        duration = null;
        if (text.IsEmpty)
        {
            return 0;
        }

        var index = 0;
        var negative = false;
        if (text[0] is '+' or '-')
        {
            negative = text[0] == '-';
            index = 1;
        }

        var numberStart = index;
        index = ConsumeDigits(text, index);
        if (index == numberStart)
        {
            return 0;
        }

        if (index < text.Length && text[index] == '.')
        {
            var afterFraction = ConsumeDigits(text, index + 1);
            if (afterFraction > index + 1)
            {
                index = afterFraction;
            }
        }

        if (negative)
        {
            return index;
        }

        var token = text[..index];
        if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            return 0;
        }

        if (seconds > TimeSpan.MaxValue.TotalSeconds)
        {
            throw new PlaylistFormatException("Playlist entry duration is outside the supported range.");
        }

        duration = TimeSpan.FromSeconds(seconds);
        return index;
    }

    private static int ConsumeDigits(ReadOnlySpan<char> text, int index)
    {
        while (index < text.Length && char.IsAsciiDigit(text[index]))
        {
            index++;
        }

        return index;
    }

    private static int IndexOfCommaOutsideQuotes(ReadOnlySpan<char> text)
    {
        var inQuotes = false;
        for (var index = 0; index < text.Length; index++)
        {
            var current = text[index];
            if (current == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (current == ',' && !inQuotes)
            {
                return index;
            }
        }

        return -1;
    }

    private static void ReadAttributes(string region, Dictionary<string, string> target)
    {
        foreach (Match match in AttributePattern().Matches(region))
        {
            target[match.Groups[1].Value] = match.Groups[2].Value;
        }
    }

    private static FrozenDictionary<string, string> Publish(Dictionary<string, string>? attributes)
    {
        if (attributes is null || attributes.Count == 0)
        {
            return FrozenDictionary<string, string>.Empty;
        }

        return attributes.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static string? Optional(FrozenDictionary<string, string> attributes, string key)
    {
        if (!attributes.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value;
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    private static bool IsExtM3u(string line, out string rest)
    {
        const string Tag = "#EXTM3U";
        if (!line.StartsWith(Tag, StringComparison.OrdinalIgnoreCase))
        {
            rest = string.Empty;
            return false;
        }

        if (line.Length == Tag.Length)
        {
            rest = string.Empty;
            return true;
        }

        if (char.IsWhiteSpace(line[Tag.Length]))
        {
            rest = line[Tag.Length..];
            return true;
        }

        rest = string.Empty;
        return false;
    }

    private static bool TryMatchPrefix(string line, string prefix, out string rest)
    {
        if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            rest = line[prefix.Length..];
            return true;
        }

        rest = string.Empty;
        return false;
    }

    [GeneratedRegex(
        @"([A-Za-z0-9_-]+)\s*=\s*""([^""]*)""",
        RegexOptions.CultureInvariant)]
    private static partial Regex AttributePattern();
}
