using Microsoft.Extensions.Logging.Abstractions;
using Rulebook.Context;
using Rulebook.Features.ABTesting;
using Rulebook.Operators;
using Rulebook.Parsing;

namespace Rulebook.UnitTests;

public class DecisionEngineTests
{
    private readonly DecisionEngine _engine;
    private readonly ExpressionParser _parser = new();

    public DecisionEngineTests()
    {
        _engine = new DecisionEngine(
            _parser,
            new OperatorRegistry(),
            new DeterministicBucketAssigner(),
            NullLogger<DecisionEngine>.Instance);
    }

    [Fact]
    public async Task EvaluateRuleAsync_SimpleCondition_ReturnsTrue()
    {
        var rule = _parser.Parse("player.level > 10");
        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["player.level"] = 42
        });

        var result = await _engine.EvaluateRuleAsync(rule, ctx);
        Assert.True(result);
    }

    [Fact]
    public async Task EvaluateRuleAsync_SimpleCondition_ReturnsFalse()
    {
        var rule = _parser.Parse("player.level > 10");
        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["player.level"] = 5
        });

        var result = await _engine.EvaluateRuleAsync(rule, ctx);
        Assert.False(result);
    }

    [Fact]
    public async Task EvaluateRuleAsync_AndCondition_BothMustMatch()
    {
        var rule = _parser.Parse("player.level > 10 && player.country == \"US\"");
        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["player.level"] = 42,
            ["player.country"] = "US"
        });

        Assert.True(await _engine.EvaluateRuleAsync(rule, ctx));
    }

    [Fact]
    public async Task EvaluateRuleAsync_AndCondition_OneFails()
    {
        var rule = _parser.Parse("player.level > 10 && player.country == \"US\"");
        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["player.level"] = 42,
            ["player.country"] = "UK"
        });

        Assert.False(await _engine.EvaluateRuleAsync(rule, ctx));
    }

    [Fact]
    public async Task EvaluateRuleAsync_OrCondition_OneMatchSuffices()
    {
        var rule = _parser.Parse("player.vip == true || player.level > 50");
        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["player.vip"] = false,
            ["player.level"] = 60
        });

        Assert.True(await _engine.EvaluateRuleAsync(rule, ctx));
    }

    [Fact]
    public async Task EvaluateSegmentAsync_MatchingRule_ReturnsMatch()
    {
        var segment = new SegmentDefinition("whales", "Whales",
            _parser.Parse("player.spent > 100"));
        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["player.spent"] = 500.0
        });

        var result = await _engine.EvaluateSegmentAsync(segment, ctx);
        Assert.True(result.IsMatch);
    }

    [Fact]
    public async Task EvaluateSegmentAsync_NonMatching_ReturnsNoMatch()
    {
        var segment = new SegmentDefinition("whales", "Whales",
            _parser.Parse("player.spent > 100"));
        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["player.spent"] = 10.0
        });

        var result = await _engine.EvaluateSegmentAsync(segment, ctx);
        Assert.False(result.IsMatch);
    }

    [Fact]
    public async Task EvaluateFeatureFlagAsync_FullRollout_ReturnsMatch()
    {
        var flag = new FeatureFlagDefinition("new-ui", "New UI",
            _parser.Parse("player.level > 0"), 100.0);
        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["player.level"] = 1,
            ["subject.id"] = "user-123"
        });

        var result = await _engine.EvaluateFeatureFlagAsync(flag, ctx);
        Assert.True(result.IsMatch);
    }

    [Fact]
    public async Task EvaluateFeatureFlagAsync_RuleNotMatched_ReturnsNoMatch()
    {
        var flag = new FeatureFlagDefinition("new-ui", "New UI",
            _parser.Parse("player.level > 100"), 100.0);
        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["player.level"] = 1,
            ["subject.id"] = "user-123"
        });

        var result = await _engine.EvaluateFeatureFlagAsync(flag, ctx);
        Assert.False(result.IsMatch);
    }

    [Fact]
    public async Task EvaluateExperimentAsync_EligibleUser_ReturnsVariant()
    {
        var experiment = new ExperimentDefinition(
            "button-color", "Button Color",
            null,
            [
                new Variant("red", 50),
                new Variant("blue", 50)
            ]);

        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["subject.id"] = "user-123"
        });

        var result = await _engine.EvaluateExperimentAsync(experiment, ctx);
        Assert.True(result.IsMatch);
        Assert.NotNull(result.VariantId);
        Assert.Contains(result.VariantId, new[] { "red", "blue" });
    }

    [Fact]
    public async Task EvaluateExperimentAsync_Deterministic_SameResult()
    {
        var experiment = new ExperimentDefinition(
            "button-color", "Button Color",
            null,
            [
                new Variant("red", 50),
                new Variant("blue", 50)
            ]);

        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["subject.id"] = "user-456"
        });

        var result1 = await _engine.EvaluateExperimentAsync(experiment, ctx);
        var result2 = await _engine.EvaluateExperimentAsync(experiment, ctx);

        Assert.Equal(result1.VariantId, result2.VariantId);
    }

    [Fact]
    public async Task EvaluateExperimentAsync_NotEligible_ReturnsNoMatch()
    {
        var experiment = new ExperimentDefinition(
            "button-color", "Button Color",
            _parser.Parse("player.level > 10"),
            [
                new Variant("red", 50),
                new Variant("blue", 50)
            ]);

        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["player.level"] = 5,
            ["subject.id"] = "user-123"
        });

        var result = await _engine.EvaluateExperimentAsync(experiment, ctx);
        Assert.False(result.IsMatch);
    }

    [Fact]
    public async Task EvaluateAsync_DecisionWithVariants_ReturnsVariant()
    {
        var definition = new DecisionDefinition(
            "promo-1", "Promo Decision",
            _parser.Parse("player.level > 5"),
            [
                new Variant("offer-A", 50, new Dictionary<string, object> { ["discount"] = 10 }),
                new Variant("offer-B", 50, new Dictionary<string, object> { ["discount"] = 20 })
            ]);

        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["player.level"] = 10,
            ["subject.id"] = "user-789"
        });

        var result = await _engine.EvaluateAsync(definition, ctx);
        Assert.True(result.IsMatch);
        Assert.NotNull(result.VariantId);
        Assert.NotNull(result.Payload);
    }

    [Fact]
    public async Task EvaluateAsync_DecisionNoMatch_ReturnsNoMatch()
    {
        var definition = new DecisionDefinition(
            "promo-1", "Promo Decision",
            _parser.Parse("player.level > 50"));

        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["player.level"] = 10
        });

        var result = await _engine.EvaluateAsync(definition, ctx);
        Assert.False(result.IsMatch);
    }

    [Fact]
    public async Task EvaluateRuleAsync_InOperator_Works()
    {
        var rule = _parser.Parse("player.country IN [\"US\", \"UK\"]");
        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["player.country"] = "US"
        });

        Assert.True(await _engine.EvaluateRuleAsync(rule, ctx));
    }

    [Fact]
    public async Task EvaluateRuleAsync_NestedContext_Works()
    {
        var rule = _parser.Parse("session.daysSinceLastLogin > 7");
        var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
        {
            ["session"] = new Dictionary<string, object?>
            {
                ["daysSinceLastLogin"] = 14
            }
        });

        Assert.True(await _engine.EvaluateRuleAsync(rule, ctx));
    }
}
