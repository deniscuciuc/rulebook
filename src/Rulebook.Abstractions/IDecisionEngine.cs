namespace Rulebook;

public interface IDecisionEngine
{
    ValueTask<DecisionResult> EvaluateAsync(
        DecisionDefinition definition,
        IDecisionContext context,
        CancellationToken ct = default);

    ValueTask<bool> EvaluateRuleAsync(
        RuleExpression rule,
        IDecisionContext context,
        CancellationToken ct = default);

    ValueTask<DecisionResult> EvaluateSegmentAsync(
        SegmentDefinition segment,
        IDecisionContext context,
        CancellationToken ct = default);

    ValueTask<DecisionResult> EvaluateFeatureFlagAsync(
        FeatureFlagDefinition flag,
        IDecisionContext context,
        CancellationToken ct = default);

    ValueTask<DecisionResult> EvaluateExperimentAsync(
        ExperimentDefinition experiment,
        IDecisionContext context,
        CancellationToken ct = default);
}
