# Getting Started

## Installation

Add the NuGet package:

```bash
dotnet add package Rulebook
```

`Rulebook.Abstractions` comes with it. Reference only `Rulebook.Abstractions` from a
project that describes decisions but does not evaluate them.

## Setup (DI)

```csharp
services.AddDecisions();
```

This registers:
- `IDecisionEngine` → `DecisionEngine`
- `IExpressionParser` → `ExpressionParser`
- `IOperatorRegistry` → `OperatorRegistry` (with built-in operators)
- `IBucketAssigner` → `DeterministicBucketAssigner`
- `SegmentEvaluator`, `FeatureFlagEvaluator`, `ExperimentEvaluator`

## Quick Example

```csharp
var engine = provider.GetRequiredService<IDecisionEngine>();
var parser = provider.GetRequiredService<IExpressionParser>();

// Build context
var context = new DictionaryDecisionContext(new Dictionary<string, object?>
{
    ["player.level"] = 25,
    ["player.country"] = "US",
    ["subject.id"] = "user-42"
});

// Evaluate a rule
var rule = parser.Parse("player.level > 10 && player.country == \"US\"");
var match = await engine.EvaluateRuleAsync(rule, context);
// match = true
```

## Definitions from configuration

Rules do not have to be built in code. Put them in `appsettings.json` as expression strings
and Rulebook parses them:

```json
{
  "Rulebook": {
    "FeatureFlags": [
      {
        "Id": "new-checkout",
        "Name": "New checkout",
        "Rule": "user.tier == 'gold' && user.country == 'US'",
        "RolloutPercentage": 25
      }
    ],
    "Experiments": [
      {
        "Id": "button-colour",
        "Name": "Button colour",
        "Variants": [
          { "Id": "blue", "Weight": 50 },
          { "Id": "green", "Weight": 50 }
        ]
      }
    ]
  }
}
```

```csharp
services.AddDecisions();
services.AddDecisionDefinitions(builder.Configuration);
```

Then inject `IDecisionDefinitionProvider` and ask it for definitions by id.

This goes through `IOptionsMonitor`, so any reloadable configuration source gives you hot
reload for free — edit the rule, and the next evaluation uses it without a restart. A rule
that fails to parse throws on first use, naming the definition, rather than silently
evaluating to false.

### A definition source of your own

To load definitions from a database, a feature-flag service, or your own configuration
framework, implement `IDecisionDefinitionProvider` and register it:

```csharp
services.AddDecisionDefinitionProvider<MyDefinitionProvider>();
```

Registration is `TryAdd`, so yours wins over the built-in one regardless of order.

## Custom Operators

```csharp
services.AddDecisions(operators =>
{
    operators.Register(new MyCustomOperator());
});
```
