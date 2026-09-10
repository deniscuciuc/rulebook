using System.Collections.Frozen;
using Microsoft.Extensions.Options;

namespace Rulebook.Definitions;

/// <summary>
/// Serves definitions from <see cref="IOptionsMonitor{TOptions}"/>, parsing each rule
/// expression once per configuration revision.
/// </summary>
/// <remarks>
/// Because it reads through <c>IOptionsMonitor</c>, any reloadable configuration source
/// gives hot reload with no extra code: change the rule in <c>appsettings.json</c> and the
/// next evaluation uses it. A steady-state lookup is a frozen-dictionary probe — the parse
/// happens only when the underlying set instance actually changes.
/// </remarks>
public sealed class OptionsDecisionDefinitionProvider : IDecisionDefinitionProvider
{
    private readonly IOptionsMonitor<DecisionDefinitionSet> _monitor;
    private readonly IExpressionParser _parser;
    private readonly Lock _gate = new();

    private DecisionDefinitionSet? _lastSet;
    private Compiled? _compiled;

    public OptionsDecisionDefinitionProvider(
        IOptionsMonitor<DecisionDefinitionSet> monitor,
        IExpressionParser parser)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(parser);

        _monitor = monitor;
        _parser = parser;
    }

    public Task<DecisionDefinition?> GetDefinitionAsync(string id, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        Current().DecisionsById.TryGetValue(id, out var match);
        return Task.FromResult(match);
    }

    public Task<IReadOnlyList<DecisionDefinition>> GetAllDefinitionsAsync(CancellationToken ct = default)
        => Task.FromResult(Current().Decisions);

    public Task<IReadOnlyList<SegmentDefinition>> GetSegmentsAsync(CancellationToken ct = default)
        => Task.FromResult(Current().Segments);

    public Task<IReadOnlyList<FeatureFlagDefinition>> GetFeatureFlagsAsync(CancellationToken ct = default)
        => Task.FromResult(Current().FeatureFlags);

    public Task<IReadOnlyList<ExperimentDefinition>> GetExperimentsAsync(CancellationToken ct = default)
        => Task.FromResult(Current().Experiments);

    private Compiled Current()
    {
        var set = _monitor.CurrentValue;

        var compiled = _compiled;
        if (compiled is not null && ReferenceEquals(set, _lastSet))
            return compiled;

        lock (_gate)
        {
            if (_compiled is not null && ReferenceEquals(set, _lastSet))
                return _compiled;

            var built = Compile(set);
            _compiled = built;
            _lastSet = set;
            return built;
        }
    }

    private Compiled Compile(DecisionDefinitionSet set)
    {
        var decisions = set.Decisions
            .Select(d => new DecisionDefinition(
                d.Id,
                d.Description,
                ParseRequired(d.Rule, "decision", d.Id),
                d.Variants.Count == 0 ? null : [.. d.Variants.Select(ToVariant)]))
            .ToList();

        var segments = set.Segments
            .Select(s => new SegmentDefinition(s.Id, s.Name, ParseRequired(s.Rule, "segment", s.Id)))
            .ToList();

        var flags = set.FeatureFlags
            .Select(f => new FeatureFlagDefinition(
                f.Id, f.Name, ParseRequired(f.Rule, "feature flag", f.Id), f.RolloutPercentage))
            .ToList();

        var experiments = set.Experiments
            .Select(e => new ExperimentDefinition(
                e.Id,
                e.Name,
                string.IsNullOrWhiteSpace(e.EligibilityRule)
                    ? null
                    : ParseRequired(e.EligibilityRule, "experiment", e.Id),
                [.. e.Variants.Select(ToVariant)],
                e.Salt))
            .ToList();

        return new Compiled(
            decisions,
            decisions.ToFrozenDictionary(d => d.Id, StringComparer.Ordinal),
            segments,
            flags,
            experiments);
    }

    private RuleExpression ParseRequired(string rule, string kind, string id)
    {
        if (string.IsNullOrWhiteSpace(rule))
            throw new InvalidOperationException($"Rulebook {kind} '{id}' has no rule expression.");

        if (_parser.TryParse(rule, out var parsed, out var error))
            return parsed!;

        throw new InvalidOperationException(
            $"Rulebook {kind} '{id}' has an invalid rule expression: {error}");
    }

    private static Variant ToVariant(ConfiguredVariant v)
        => new(v.Id, v.Weight, v.Payload.Count == 0 ? null : v.Payload);

    private sealed record Compiled(
        IReadOnlyList<DecisionDefinition> Decisions,
        FrozenDictionary<string, DecisionDefinition> DecisionsById,
        IReadOnlyList<SegmentDefinition> Segments,
        IReadOnlyList<FeatureFlagDefinition> FeatureFlags,
        IReadOnlyList<ExperimentDefinition> Experiments);
}
