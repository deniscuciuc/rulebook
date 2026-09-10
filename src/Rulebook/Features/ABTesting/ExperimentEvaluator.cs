namespace Rulebook.Features.ABTesting;

public sealed class ExperimentEvaluator(IDecisionEngine engine)
{
    public ValueTask<DecisionResult> GetVariantAsync(
        ExperimentDefinition experiment,
        IDecisionContext context,
        CancellationToken ct = default)
    {
        return engine.EvaluateExperimentAsync(experiment, context, ct);
    }
}
