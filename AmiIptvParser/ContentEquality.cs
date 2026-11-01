using System.Collections.Immutable;

namespace AmiIptvParser;

/// <summary>
/// Content comparison for collections whose BCL <c>Equals</c> follows the backing store.
/// </summary>
internal static class ContentEquality
{
    public static bool DictionaryEquals(
        ImmutableDictionary<string, string> left,
        ImmutableDictionary<string, string> right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (var pair in left)
        {
            if (!ContainsExact(right, pair.Key, pair.Value))
            {
                return false;
            }
        }

        return true;
    }

    public static int DictionaryHash(ImmutableDictionary<string, string> values)
    {
        var hash = values.Count;
        foreach (var pair in values)
        {
            hash = unchecked(hash ^ HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(pair.Key),
                StringComparer.Ordinal.GetHashCode(pair.Value)));
        }

        return hash;
    }

    public static bool EntriesEqual(ImmutableArray<PlaylistEntry> left, ImmutableArray<PlaylistEntry> right)
    {
        if (left.IsDefault || right.IsDefault)
        {
            return left.IsDefault && right.IsDefault;
        }

        if (left.Length != right.Length)
        {
            return false;
        }

        for (var index = 0; index < left.Length; index++)
        {
            if (left[index] != right[index])
            {
                return false;
            }
        }

        return true;
    }

    public static int EntriesHash(ImmutableArray<PlaylistEntry> entries)
    {
        if (entries.IsDefault)
        {
            return 0;
        }

        var hash = new HashCode();
        hash.Add(entries.Length);
        foreach (var entry in entries)
        {
            hash.Add(entry);
        }

        return hash.ToHashCode();
    }

    private static bool ContainsExact(ImmutableDictionary<string, string> values, string key, string expected)
    {
        foreach (var pair in values)
        {
            if (string.Equals(pair.Key, key, StringComparison.Ordinal)
                && string.Equals(pair.Value, expected, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
