using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rulebook.Definitions;
using Rulebook.Extensions;

namespace Rulebook.UnitTests;

/// <summary>
/// Covers the definition-source layer: binding from configuration, parsing each rule
/// expression, serving through <see cref="IDecisionDefinitionProvider"/>, and picking up a
/// configuration reload without a restart.
/// </summary>
public sealed class DecisionDefinitionProviderTests
{
    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddDecisions();
        services.AddDecisionDefinitions(configuration);
        return services.BuildServiceProvider();
    }

    private static Dictionary<string, string?> Sample() => new()
    {
        ["Rulebook:Decisions:0:Id"] = "checkout.discount",
        ["Rulebook:Decisions:0:Description"] = "Loyalty discount",
        ["Rulebook:Decisions:0:Rule"] = "user.tier == 'gold'",

        ["Rulebook:Segments:0:Id"] = "gold",
        ["Rulebook:Segments:0:Name"] = "Gold tier",
        ["Rulebook:Segments:0:Rule"] = "user.tier == 'gold'",

        ["Rulebook:FeatureFlags:0:Id"] = "new-ui",
        ["Rulebook:FeatureFlags:0:Name"] = "New UI",
        ["Rulebook:FeatureFlags:0:Rule"] = "user.beta == true",
        ["Rulebook:FeatureFlags:0:RolloutPercentage"] = "25",

        ["Rulebook:Experiments:0:Id"] = "button-colour",
        ["Rulebook:Experiments:0:Name"] = "Button colour",
        ["Rulebook:Experiments:0:Variants:0:Id"] = "blue",
        ["Rulebook:Experiments:0:Variants:0:Weight"] = "50",
        ["Rulebook:Experiments:0:Variants:1:Id"] = "green",
        ["Rulebook:Experiments:0:Variants:1:Weight"] = "50",
    };

    [Fact]
    public async Task GetDefinitionAsync_ReturnsMatchingDecision_WithItsRuleParsed()
    {
        var provider = Build(Sample()).GetRequiredService<IDecisionDefinitionProvider>();

        var found = await provider.GetDefinitionAsync("checkout.discount");

        Assert.NotNull(found);
        Assert.Equal("Loyalty discount", found.Description);
        var condition = Assert.IsType<ConditionExpression>(found.Rule);
        Assert.Equal("user.tier", condition.Path);
        Assert.Equal("gold", condition.Value);
    }

    [Fact]
    public async Task GetDefinitionAsync_ReturnsNull_ForUnknownId()
    {
        var provider = Build(Sample()).GetRequiredService<IDecisionDefinitionProvider>();

        Assert.Null(await provider.GetDefinitionAsync("nope"));
    }

    [Fact]
    public async Task GetDefinitionAsync_IsCaseSensitive()
    {
        var provider = Build(Sample()).GetRequiredService<IDecisionDefinitionProvider>();

        Assert.Null(await provider.GetDefinitionAsync("Checkout.Discount"));
    }

    [Fact]
    public async Task Getters_ReturnEveryConfiguredCollection()
    {
        var provider = Build(Sample()).GetRequiredService<IDecisionDefinitionProvider>();

        Assert.Single(await provider.GetAllDefinitionsAsync());
        Assert.Single(await provider.GetSegmentsAsync());
        Assert.Single(await provider.GetExperimentsAsync());

        var flag = Assert.Single(await provider.GetFeatureFlagsAsync());
        Assert.Equal(25.0, flag.RolloutPercentage);
    }

    [Fact]
    public async Task Experiment_WithNoEligibilityRule_IsOpenToEveryone()
    {
        var provider = Build(Sample()).GetRequiredService<IDecisionDefinitionProvider>();

        var experiment = Assert.Single(await provider.GetExperimentsAsync());

        Assert.Null(experiment.EligibilityRule);
        Assert.Equal(2, experiment.Variants.Count);
    }

    [Fact]
    public async Task EmptyConfiguration_YieldsEmptyCollections_NotNull()
    {
        var provider = Build([]).GetRequiredService<IDecisionDefinitionProvider>();

        Assert.Empty(await provider.GetAllDefinitionsAsync());
        Assert.Empty(await provider.GetSegmentsAsync());
        Assert.Empty(await provider.GetFeatureFlagsAsync());
        Assert.Empty(await provider.GetExperimentsAsync());
        Assert.Null(await provider.GetDefinitionAsync("anything"));
    }

    [Fact]
    public async Task RepeatedLookups_ReturnTheSameParsedInstance()
    {
        var provider = Build(Sample()).GetRequiredService<IDecisionDefinitionProvider>();

        // The second call takes the cached path — parsing must not happen again.
        var first = await provider.GetDefinitionAsync("checkout.discount");
        var second = await provider.GetDefinitionAsync("checkout.discount");

        Assert.Same(first, second);
    }

    [Fact]
    public async Task InvalidRuleExpression_FailsLoudly_NamingTheDefinition()
    {
        var provider = Build(new Dictionary<string, string?>
        {
            ["Rulebook:Decisions:0:Id"] = "broken",
            ["Rulebook:Decisions:0:Rule"] = ">>>",
        }).GetRequiredService<IDecisionDefinitionProvider>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetAllDefinitionsAsync());

        Assert.Contains("broken", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingRuleExpression_FailsLoudly()
    {
        var provider = Build(new Dictionary<string, string?>
        {
            ["Rulebook:Segments:0:Id"] = "no-rule",
            ["Rulebook:Segments:0:Name"] = "No rule",
        }).GetRequiredService<IDecisionDefinitionProvider>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetSegmentsAsync());

        Assert.Contains("no-rule", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDecisionDefinitions_Delegate_RegistersTheOptionsProvider()
    {
        var services = new ServiceCollection();
        services.AddDecisions();
        services.AddDecisionDefinitions(set => set.Segments.Add(new ConfiguredSegment
        {
            Id = "everyone",
            Name = "Everyone",
            Rule = "user.id != ''",
        }));

        using var sp = services.BuildServiceProvider();

        Assert.IsType<OptionsDecisionDefinitionProvider>(sp.GetRequiredService<IDecisionDefinitionProvider>());
    }

    [Fact]
    public async Task AddDecisionDefinitions_Delegate_ServesWhatItConfigured()
    {
        var services = new ServiceCollection();
        services.AddDecisions();
        services.AddDecisionDefinitions(set => set.Segments.Add(new ConfiguredSegment
        {
            Id = "everyone",
            Name = "Everyone",
            Rule = "user.tier == 'gold'",
        }));

        using var sp = services.BuildServiceProvider();

        var segment = Assert.Single(
            await sp.GetRequiredService<IDecisionDefinitionProvider>().GetSegmentsAsync());
        Assert.Equal("Everyone", segment.Name);
    }

    [Fact]
    public void AddDecisionDefinitionProvider_LetsACustomSourceWin()
    {
        var services = new ServiceCollection();
        services.AddDecisions();
        services.AddDecisionDefinitionProvider<StubProvider>();
        services.AddDecisionDefinitions(_ => { });

        using var sp = services.BuildServiceProvider();

        Assert.IsType<StubProvider>(sp.GetRequiredService<IDecisionDefinitionProvider>());
    }

    private sealed class StubProvider : IDecisionDefinitionProvider
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
}
