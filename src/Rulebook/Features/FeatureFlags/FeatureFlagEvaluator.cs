namespace Rulebook.Features.FeatureFlags;

public sealed class FeatureFlagEvaluator(IDecisionEngine engine)
{
    public async ValueTask<bool> IsEnabledAsync(
        FeatureFlagDefinition flag,
        IDecisionContext context,
        CancellationToken ct = default)
    {
        var result = await engine.EvaluateFeatureFlagAsync(flag, context, ct).ConfigureAwait(false);
        return result.IsMatch;
    }
}
