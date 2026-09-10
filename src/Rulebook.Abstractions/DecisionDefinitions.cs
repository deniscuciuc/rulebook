namespace Rulebook;

public sealed record DecisionDefinition(
    string Id,
    string? Description,
    RuleExpression Rule,
    IReadOnlyList<Variant>? Variants = null);

public sealed record SegmentDefinition(
    string Id,
    string Name,
    RuleExpression Rule);

public sealed record FeatureFlagDefinition(
    string Id,
    string Name,
    RuleExpression Rule,
    double RolloutPercentage = 100.0);

public sealed record ExperimentDefinition(
    string Id,
    string Name,
    RuleExpression? EligibilityRule,
    IReadOnlyList<Variant> Variants,
    string? Salt = null);

public sealed record Variant(
    string Id,
    double Weight,
    IReadOnlyDictionary<string, object>? Payload = null);

public sealed record DecisionResult(
    bool IsMatch,
    string? VariantId = null,
    IReadOnlyDictionary<string, object>? Payload = null)
{
    public static readonly DecisionResult NoMatch = new(false);
    public static readonly DecisionResult Match = new(true);
}
