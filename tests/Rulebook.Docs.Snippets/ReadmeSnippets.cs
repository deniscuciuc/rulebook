using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rulebook.Context;
using Rulebook.Definitions;
using Rulebook.Extensions;

namespace Rulebook.Docs.Snippets;

internal sealed record User(string Id, string Tier, string Country);

/// <summary>README — registration.</summary>
internal static class Registration
{
    internal static void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDecisions();
        services.AddDecisionDefinitions(configuration);
    }

    internal static void CustomSource(IServiceCollection services)
    {
        services.AddDecisionDefinitionProvider<MyDefinitionProvider>();
    }
}

/// <summary>README — the quick-start checkout service.</summary>
internal sealed class CheckoutService(
    IDecisionEngine engine,
    IDecisionDefinitionProvider definitions)
{
    public async Task<bool> UseNewCheckoutAsync(User user, CancellationToken cancellationToken)
    {
        var flags = await definitions.GetFeatureFlagsAsync(cancellationToken);
        var flag = flags.Single(f => f.Id == "new-checkout");

        var context = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["user.tier"] = user.Tier,
            ["user.country"] = user.Country,
            ["subject.id"] = user.Id,
        });

        var result = await engine.EvaluateFeatureFlagAsync(flag, context, cancellationToken);
        return result.IsMatch;
    }
}

/// <summary>README — building a context.</summary>
internal static class Contexts
{
    internal static DictionaryDecisionContext Build()
    {
        return new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["player.level"] = 25,
            ["player.country"] = "US",
            ["subject.id"] = "user-42",
        });
    }
}

/// <summary>docs/getting-started.md — custom operators.</summary>
internal static class Operators
{
    internal static void RegisterCustom(IServiceCollection services, IOperator myCustomOperator)
    {
        services.AddDecisions(operators => operators.Register(myCustomOperator));
    }
}

/// <summary>A stand-in for a definition source of your own.</summary>
internal sealed class MyDefinitionProvider : IDecisionDefinitionProvider
{
    public Task<DecisionDefinition?> GetDefinitionAsync(string id, CancellationToken ct = default)
        => Task.FromResult<DecisionDefinition?>(null);

    public Task<IReadOnlyList<DecisionDefinition>> GetAllDefinitionsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DecisionDefinition>>([]);

    public Task<IReadOnlyList<SegmentDefinition>> GetSegmentsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<SegmentDefinition>>([]);

    public Task<IReadOnlyList<FeatureFlagDefinition>> GetFeatureFlagsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<FeatureFlagDefinition>>([]);

    public Task<IReadOnlyList<ExperimentDefinition>> GetExperimentsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ExperimentDefinition>>([]);
}
