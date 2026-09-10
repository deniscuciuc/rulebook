namespace Rulebook.Context;

/// <summary>
/// A pre-bound accessor that resolves a specific context path.
/// Created once per path, then reused for every evaluation — avoids repeated string lookups.
/// </summary>
public readonly struct BoundAccessor
{
    private readonly string _path;

    public BoundAccessor(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
    }

    public string Path => _path;

    /// <summary>
    /// Resolves the value from the context. Returns false if the path is not found.
    /// </summary>
    public bool TryGetValue(IDecisionContext context, out object? value)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TryGetValue(_path, out value);
    }
}

/// <summary>
/// Caches bound accessors for context paths used by a rule expression.
/// Pre-extracts all paths from the expression tree at compile time.
/// </summary>
public static class BoundAccessorFactory
{
    /// <summary>
    /// Extracts all unique context paths from an expression tree.
    /// </summary>
    public static IReadOnlyList<string> ExtractPaths(RuleExpression expression)
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);
        CollectPaths(expression, paths);
        return [.. paths];
    }

    private static void CollectPaths(RuleExpression expression, HashSet<string> paths)
    {
        switch (expression)
        {
            case ConditionExpression condition:
                paths.Add(condition.Path);
                break;
            case GroupExpression group:
                foreach (var child in group.Children)
                    CollectPaths(child, paths);
                break;
            case NotExpression not:
                CollectPaths(not.Inner, paths);
                break;
        }
    }
}
