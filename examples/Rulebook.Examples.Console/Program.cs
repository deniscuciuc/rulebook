using Microsoft.Extensions.DependencyInjection;
using Rulebook;
using Rulebook.Context;
using Rulebook.Extensions;
using Rulebook.Features.ABTesting;
using Rulebook.Features.FeatureFlags;
using Rulebook.Features.Segmentation;
using Rulebook.Parsing;

// ─── Setup DI ───
var services = new ServiceCollection();

services.AddLogging();
services.AddDecisions();

var provider = services.BuildServiceProvider();
var engine = provider.GetRequiredService<IDecisionEngine>();
var parser = provider.GetRequiredService<IExpressionParser>();
var segmentEval = provider.GetRequiredService<SegmentEvaluator>();
var flagEval = provider.GetRequiredService<FeatureFlagEvaluator>();
var experimentEval = provider.GetRequiredService<ExperimentEvaluator>();

// ─── Build context (simulating a game player) ───
var context = new DictionaryDecisionContext(new Dictionary<string, object?>
{
    ["subject.id"] = "player-42",
    ["player.level"] = 25,
    ["player.spent"] = 150.0,
    ["player.country"] = "US",
    ["player.vip"] = true,
    ["session.daysSinceLastLogin"] = 3
});

Console.WriteLine("=== Rulebook — Example ===\n");

// ─── 1. Segmentation ───
Console.WriteLine("--- Segmentation ---");

var whaleSegment = new SegmentDefinition("whales", "Whales",
    parser.Parse("player.spent > 100"));

var newPlayerSegment = new SegmentDefinition("new-players", "New Players",
    parser.Parse("player.level < 5"));

var isWhale = await segmentEval.IsMatchAsync(whaleSegment, context);
var isNew = await segmentEval.IsMatchAsync(newPlayerSegment, context);

Console.WriteLine($"  Whale segment: {isWhale}"); // true
Console.WriteLine($"  New player segment: {isNew}"); // false

// ─── 2. Feature Flags ───
Console.WriteLine("\n--- Feature Flags ---");

var newUiFlag = new FeatureFlagDefinition("new-ui", "New UI",
    parser.Parse("player.level > 10"), RolloutPercentage: 50.0);

var isEnabled = await flagEval.IsEnabledAsync(newUiFlag, context);
Console.WriteLine($"  New UI flag enabled: {isEnabled}");

// ─── 3. A/B Testing ───
Console.WriteLine("\n--- A/B Testing ---");

var experiment = new ExperimentDefinition(
    "button-color", "Button Color Test",
    EligibilityRule: parser.Parse("player.level > 5"),
    Variants:
    [
        new Variant("red", 33, new Dictionary<string, object> { ["hex"] = "#FF0000" }),
        new Variant("blue", 33, new Dictionary<string, object> { ["hex"] = "#0000FF" }),
        new Variant("green", 34, new Dictionary<string, object> { ["hex"] = "#00FF00" })
    ]);

var experimentResult = await experimentEval.GetVariantAsync(experiment, context);
Console.WriteLine($"  Experiment match: {experimentResult.IsMatch}");
Console.WriteLine($"  Assigned variant: {experimentResult.VariantId}");
Console.WriteLine(
    $"  Payload: {(experimentResult.Payload != null ? string.Join(", ", experimentResult.Payload.Select(kv => $"{kv.Key}={kv.Value}")) : "none")}");

// ─── 4. Complex rule evaluation ───
Console.WriteLine("\n--- Complex Rules ---");

var complexRule = parser.Parse(
    "(player.spent > 100 && player.country == \"US\") || player.vip == true");
var complexResult = await engine.EvaluateRuleAsync(complexRule, context);
Console.WriteLine($"  Complex rule: {complexResult}"); // true

// ─── 5. Decision with variants ───
Console.WriteLine("\n--- Decision with Variants ---");

var promoDecision = new DecisionDefinition(
    "summer-promo", "Summer Promotion",
    parser.Parse("player.level > 10 && player.country IN [\"US\", \"UK\", \"DE\"]"),
    Variants:
    [
        new Variant("big-offer", 30, new Dictionary<string, object> { ["discount"] = 20 }),
        new Variant("small-offer", 70, new Dictionary<string, object> { ["discount"] = 5 })
    ]);

var promoResult = await engine.EvaluateAsync(promoDecision, context);
Console.WriteLine($"  Match: {promoResult.IsMatch}");
Console.WriteLine($"  Variant: {promoResult.VariantId}");

// ─── 6. Determinism demo ───
Console.WriteLine("\n--- Determinism ---");
for (var i = 0; i < 3; i++)
{
    var r = await engine.EvaluateExperimentAsync(experiment, context);
    Console.WriteLine($"  Run {i + 1}: {r.VariantId}");
}

Console.WriteLine("\nDone.");
