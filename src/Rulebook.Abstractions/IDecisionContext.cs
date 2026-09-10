namespace Rulebook;

public interface IDecisionContext
{
    bool TryGetValue(string path, out object? value);

    ValueTask<object?> GetValueAsync(string path, CancellationToken ct = default);
}
