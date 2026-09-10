using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Rulebook.Compilation;
using Rulebook.Engine;
using Rulebook.Features.ABTesting;

namespace Rulebook;

public sealed class DecisionEngine : IDecisionEngine
{
    private const string SubjectIdPath = "subject.id";

    private readonly RuleEvaluator _evaluator;
    private readonly RuleCompiler _compiler;
    private readonly IBucketAssigner _bucketAssigner;
    private readonly ConcurrentDictionary<IReadOnlyList<Variant>, CumulativeWeightTable> _weightTables = new();

    public DecisionEngine(
        IExpressionParser parser,
        IOperatorRegistry operators,
        IBucketAssigner bucketAssigner,
        ILogger<DecisionEngine> logger)
    {
        _evaluator = new RuleEvaluator(operators, logger);
        _compiler = new RuleCompiler(operators);
        _bucketAssigner = bucketAssigner;
    }

    public ValueTask<DecisionResult> EvaluateAsync(
        DecisionDefinition definition,
        IDecisionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(definition);

        // Compiled fast path: zero tree-walking overhead
        var compiled = _compiler.Compile(definition.Rule);
        var match = compiled.Evaluate(context);

        if (!match)
            return new ValueTask<DecisionResult>(DecisionResult.NoMatch);

        if (definition.Variants is { Count: > 0 })
        {
            var subjectId = ResolveSubjectIdSync(context);
            if (subjectId is not null)
            {
                var variant = ResolveVariantFast(definition.Id, subjectId, definition.Variants);
                return new ValueTask<DecisionResult>(new DecisionResult(true, variant?.Id, variant?.Payload));
            }

            return EvaluateAsyncCore(definition, context, ct);
        }

        return new ValueTask<DecisionResult>(DecisionResult.Match);
    }

    private async ValueTask<DecisionResult> EvaluateAsyncCore(
        DecisionDefinition definition,
        IDecisionContext context,
        CancellationToken ct)
    {
        var match = await _evaluator.EvaluateAsync(definition.Rule, context, ct).ConfigureAwait(false);

        if (!match)
            return DecisionResult.NoMatch;

        if (definition.Variants is { Count: > 0 })
        {
            var subjectId = await ResolveSubjectIdAsync(context, ct).ConfigureAwait(false);
            var variant = ResolveVariantFast(definition.Id, subjectId, definition.Variants);
            return new DecisionResult(true, variant?.Id, variant?.Payload);
        }

        return DecisionResult.Match;
    }

    public ValueTask<bool> EvaluateRuleAsync(
        RuleExpression rule,
        IDecisionContext context,
        CancellationToken ct = default)
    {
        // Compiled fast path
        var compiled = _compiler.Compile(rule);
        return new ValueTask<bool>(compiled.Evaluate(context));
    }

    public ValueTask<DecisionResult> EvaluateSegmentAsync(
        SegmentDefinition segment,
        IDecisionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(segment);

        var compiled = _compiler.Compile(segment.Rule);
        var match = compiled.Evaluate(context);
        return new ValueTask<DecisionResult>(match ? DecisionResult.Match : DecisionResult.NoMatch);
    }

    public ValueTask<DecisionResult> EvaluateFeatureFlagAsync(
        FeatureFlagDefinition flag,
        IDecisionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(flag);

        var compiled = _compiler.Compile(flag.Rule);
        var match = compiled.Evaluate(context);

        if (!match)
            return new ValueTask<DecisionResult>(DecisionResult.NoMatch);

        if (flag.RolloutPercentage >= 100.0)
            return new ValueTask<DecisionResult>(DecisionResult.Match);

        var subjectId = ResolveSubjectIdSync(context);
        if (subjectId is not null)
        {
            var bucket = _bucketAssigner.AssignBucket(flag.Id, subjectId);
            return new ValueTask<DecisionResult>(bucket >= flag.RolloutPercentage
                ? DecisionResult.NoMatch
                : DecisionResult.Match);
        }

        return EvaluateFeatureFlagAsyncCore(flag, context, ct);
    }

    private async ValueTask<DecisionResult> EvaluateFeatureFlagAsyncCore(
        FeatureFlagDefinition flag,
        IDecisionContext context,
        CancellationToken ct)
    {
        var match = await _evaluator.EvaluateAsync(flag.Rule, context, ct).ConfigureAwait(false);
        if (!match) return DecisionResult.NoMatch;

        if (flag.RolloutPercentage < 100.0)
        {
            var subjectId = await ResolveSubjectIdAsync(context, ct).ConfigureAwait(false);
            var bucket = _bucketAssigner.AssignBucket(flag.Id, subjectId);
            if (bucket >= flag.RolloutPercentage) return DecisionResult.NoMatch;
        }

        return DecisionResult.Match;
    }

    public ValueTask<DecisionResult> EvaluateExperimentAsync(
        ExperimentDefinition experiment,
        IDecisionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(experiment);

        // Compiled fast path for eligibility
        if (experiment.EligibilityRule is not null)
        {
            var compiled = _compiler.Compile(experiment.EligibilityRule);
            if (!compiled.Evaluate(context))
                return new ValueTask<DecisionResult>(DecisionResult.NoMatch);
        }

        var subjectId = ResolveSubjectIdSync(context);
        if (subjectId is not null)
        {
            var variant = ResolveVariantFast(experiment.Salt ?? experiment.Id, subjectId, experiment.Variants);
            return variant is null
                ? new ValueTask<DecisionResult>(DecisionResult.NoMatch)
                : new ValueTask<DecisionResult>(new DecisionResult(true, variant.Id, variant.Payload));
        }

        return EvaluateExperimentAsyncCore(experiment, context, ct);
    }

    private async ValueTask<DecisionResult> EvaluateExperimentAsyncCore(
        ExperimentDefinition experiment,
        IDecisionContext context,
        CancellationToken ct)
    {
        if (experiment.EligibilityRule is not null)
        {
            var eligible = await _evaluator.EvaluateAsync(experiment.EligibilityRule, context, ct).ConfigureAwait(false);
            if (!eligible) return DecisionResult.NoMatch;
        }

        var subjectId = await ResolveSubjectIdAsync(context, ct).ConfigureAwait(false);
        var variant = ResolveVariantFast(experiment.Salt ?? experiment.Id, subjectId, experiment.Variants);

        return variant is null
            ? DecisionResult.NoMatch
            : new DecisionResult(true, variant.Id, variant.Payload);
    }

    private static string? ResolveSubjectIdSync(IDecisionContext context)
    {
        return context.TryGetValue(SubjectIdPath, out var obj) ? obj?.ToString() ?? "" : null;
    }

    private static async ValueTask<string> ResolveSubjectIdAsync(IDecisionContext context, CancellationToken ct)
    {
        if (context.TryGetValue(SubjectIdPath, out var obj))
            return obj?.ToString() ?? "";

        obj = await context.GetValueAsync(SubjectIdPath, ct).ConfigureAwait(false);
        return obj?.ToString() ?? "";
    }

    /// <summary>
    /// Uses pre-computed cumulative weight table for variant selection.
    /// </summary>
    private Variant? ResolveVariantFast(string experimentId, string subjectId, IReadOnlyList<Variant> variants)
    {
        var bucket = _bucketAssigner.AssignBucket(experimentId, subjectId);
        var table = _weightTables.GetOrAdd(variants, static v => new CumulativeWeightTable(v));
        return table.Select(bucket);
    }
}
