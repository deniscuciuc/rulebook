namespace Rulebook.Features.Segmentation;

public sealed class SegmentEvaluator(IDecisionEngine engine)
{
    public async ValueTask<bool> IsMatchAsync(
        SegmentDefinition segment,
        IDecisionContext context,
        CancellationToken ct = default)
    {
        var result = await engine.EvaluateSegmentAsync(segment, context, ct).ConfigureAwait(false);
        return result.IsMatch;
    }

    public async Task<IReadOnlyList<SegmentDefinition>> GetMatchingSegmentsAsync(
        IReadOnlyList<SegmentDefinition> segments,
        IDecisionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(segments);

        var matched = new List<SegmentDefinition>();

        foreach (var segment in segments)
            if (await IsMatchAsync(segment, context, ct).ConfigureAwait(false))
                matched.Add(segment);

        return matched;
    }
}
