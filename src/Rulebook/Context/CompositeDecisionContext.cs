namespace Rulebook.Context;

public sealed class CompositeDecisionContext(params IReadOnlyList<IDecisionContext> contexts) : IDecisionContext
{
    public bool TryGetValue(string path, out object? value)
    {
        foreach (var ctx in contexts)
        {
            if (ctx.TryGetValue(path, out value))
                return true;
        }

        value = null;
        return false;
    }

    public async ValueTask<object?> GetValueAsync(string path, CancellationToken ct = default)
    {
        if (TryGetValue(path, out var value))
            return value;

        foreach (var ctx in contexts)
        {
            var result = await ctx.GetValueAsync(path, ct).ConfigureAwait(false);
            if (result is not null)
                return result;
        }

        return null;
    }
}
