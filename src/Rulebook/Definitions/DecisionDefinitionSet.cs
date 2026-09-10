namespace Rulebook.Definitions;

/// <summary>
/// Decisions, segments, feature flags and experiments as they appear in configuration.
/// </summary>
/// <remarks>
/// Rules are held as **expression strings** rather than as <see cref="RuleExpression"/>
/// trees, because a configuration binder cannot construct a polymorphic type — and because
/// <c>"user.tier == 'gold' and cart.total &gt; 100"</c> is the readable form in an
/// <c>appsettings.json</c>. <see cref="OptionsDecisionDefinitionProvider"/> parses them.
/// </remarks>
public sealed class DecisionDefinitionSet
{
    /// <summary>Configuration section this set binds to by default.</summary>
    public const string SectionPath = "Rulebook";

    public IList<ConfiguredDecision> Decisions { get; init; } = [];

    public IList<ConfiguredSegment> Segments { get; init; } = [];

    public IList<ConfiguredFeatureFlag> FeatureFlags { get; init; } = [];

    public IList<ConfiguredExperiment> Experiments { get; init; } = [];
}

/// <summary>A decision as configured, with its rule as an expression string.</summary>
public sealed class ConfiguredDecision
{
    public string Id { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Rule expression, e.g. <c>user.tier == 'gold'</c>.</summary>
    public string Rule { get; set; } = string.Empty;

    public IList<ConfiguredVariant> Variants { get; init; } = [];
}

/// <summary>A segment as configured.</summary>
public sealed class ConfiguredSegment
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Rule { get; set; } = string.Empty;
}

/// <summary>A feature flag as configured.</summary>
public sealed class ConfiguredFeatureFlag
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Rule { get; set; } = string.Empty;

    public double RolloutPercentage { get; set; } = 100.0;
}

/// <summary>An experiment as configured.</summary>
public sealed class ConfiguredExperiment
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Optional eligibility rule. Empty means everyone is eligible.</summary>
    public string? EligibilityRule { get; set; }

    public IList<ConfiguredVariant> Variants { get; init; } = [];

    public string? Salt { get; set; }
}

/// <summary>A variant as configured.</summary>
public sealed class ConfiguredVariant
{
    public string Id { get; set; } = string.Empty;

    public double Weight { get; set; }

    public Dictionary<string, object> Payload { get; init; } = [];
}
