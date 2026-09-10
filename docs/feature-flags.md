# Feature Flags

## Overview

Boolean evaluation with optional percentage-based rollout. Rules determine eligibility; rollout percentage controls gradual release.

## Define a Feature Flag

```csharp
var flag = new FeatureFlagDefinition(
    Id: "new-ui",
    Name: "New UI",
    Rule: parser.Parse("player.level > 10"),
    RolloutPercentage: 50.0  // 50% of matching users
);
```

## Evaluate

```csharp
var result = await engine.EvaluateFeatureFlagAsync(flag, context);
if (result.IsMatch)
{
    // Feature is enabled for this user
}
```

Or use the convenience wrapper:

```csharp
var flagEval = provider.GetRequiredService<FeatureFlagEvaluator>();
var enabled = await flagEval.IsEnabledAsync(flag, context);
```

## Evaluation Logic

1. Evaluate the rule against the context
2. If rule doesn't match → `NoMatch` (feature disabled)
3. If `RolloutPercentage == 100` → `Match` (feature enabled)
4. If `RolloutPercentage < 100` → use bucket assigner with `subject.id`
   - Bucket < rollout percentage → enabled
   - Bucket >= rollout percentage → disabled

## Rollout Strategy

Rollout percentage uses the same deterministic bucketing as A/B testing:

- `subject.id` is resolved from the context
- Hash determines a bucket 0-99
- Bucket is compared against the percentage threshold

This means:
- Same user always gets the same result (sticky)
- Increasing the percentage from 50% to 75% keeps the original 50% in
- No external state needed

## Full Rollout

```csharp
new FeatureFlagDefinition("feature-x", "Feature X",
    parser.Parse("player.active == true"),
    RolloutPercentage: 100.0)  // Everyone matching the rule gets it
```
