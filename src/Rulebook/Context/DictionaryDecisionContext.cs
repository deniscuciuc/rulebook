using System.Collections;

namespace Rulebook.Context;

public sealed class DictionaryDecisionContext(IReadOnlyDictionary<string, object?> data) : IDecisionContext
{
    public bool TryGetValue(string path, out object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Fast path: direct key match (covers both flat and dotted keys)
        if (data.TryGetValue(path, out value))
            return true;

        // Only attempt nested resolution if path contains a dot
        if (!path.Contains('.'))
        {
            value = null;
            return false;
        }

        // Walk nested dictionaries using span-based segmentation (zero-alloc splitting)
        object? current = data;
        var remaining = path.AsSpan();

        while (remaining.Length > 0)
        {
            ReadOnlySpan<char> segment;
            var dotIndex = remaining.IndexOf('.');
            if (dotIndex >= 0)
            {
                segment = remaining[..dotIndex];
                remaining = remaining[(dotIndex + 1)..];
            }
            else
            {
                segment = remaining;
                remaining = default;
            }

            // We need a string key for dictionary lookups — use the original path's substring
            // to avoid allocation when possible via string interning or the same string instance
            var segmentStr = segment.ToString();

            switch (current)
            {
                case null:
                    value = null;
                    return false;
                case IReadOnlyDictionary<string, object?> dict:
                    if (!dict.TryGetValue(segmentStr, out current))
                    {
                        value = null;
                        return false;
                    }
                    break;
                case IDictionary<string, object?> mutableDict:
                    if (!mutableDict.TryGetValue(segmentStr, out current))
                    {
                        value = null;
                        return false;
                    }
                    break;
                case IDictionary nonGenericDict:
                    if (!nonGenericDict.Contains(segmentStr))
                    {
                        value = null;
                        return false;
                    }
                    current = nonGenericDict[segmentStr];
                    break;
                default:
                    value = null;
                    return false;
            }
        }

        value = current;
        return true;
    }

    public ValueTask<object?> GetValueAsync(string path, CancellationToken ct = default)
    {
        TryGetValue(path, out var value);
        return ValueTask.FromResult(value);
    }
}
