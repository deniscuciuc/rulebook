# A/B Testing

## Overview

The decision engine supports deterministic A/B testing. Same user + same experiment = same variant, always. No external state required.

## How It Works

### 1. Define an Experiment

```csharp
var experiment = new ExperimentDefinition(
    Id: "checkout-flow",
    Name: "Checkout Flow Test",
    EligibilityRule: parser.Parse("player.level > 5"),  // optional
    Variants: [
        new Variant("control", 50),
        new Variant("new-flow", 50)
    ],
    Salt: null  // optional, defaults to experiment Id
);
```

### 2. Evaluate

```csharp
var result = await engine.EvaluateExperimentAsync(experiment, context);
// result.IsMatch = true/false
// result.VariantId = "control" or "new-flow"
// result.Payload = optional metadata
```

## Deterministic Bucketing

Uses `XxHash32` (System.IO.Hashing, built into .NET):

```
input  = "{experimentId}:{subjectId}"
hash   = XxHash32(UTF8(input))
bucket = hash % 100
```

Properties:
- **Deterministic**: Same inputs → same bucket, every time
- **Sticky**: User always sees the same variant for a given experiment
- **No state**: No database or cache needed
- **Fast**: XxHash32 is extremely fast
- **Well-distributed**: Uniform distribution across buckets

## Variant Selection

Variants have weights that define traffic allocation:

```csharp
new Variant("A", 50),   // 50% traffic
new Variant("B", 30),   // 30% traffic
new Variant("C", 20)    // 20% traffic
```

Bucket (0-99) is mapped to variants via cumulative weight:
- Bucket 0-49 → A
- Bucket 50-79 → B
- Bucket 80-99 → C

## Variant Payload

Attach metadata to variants:

```csharp
new Variant("red-button", 50, new Dictionary<string, object>
{
    ["color"] = "#FF0000",
    ["text"] = "Buy Now!"
})
```

## Salt

Use a custom salt to produce different bucket assignments for the same user across experiments:

```csharp
new ExperimentDefinition(
    Id: "exp-1",
    Name: "...",
    EligibilityRule: null,
    Variants: [...],
    Salt: "custom-salt-2024"  // different salt = different bucketing
)
```

## Eligibility Rules

Restrict who can participate:

```csharp
EligibilityRule: parser.Parse("player.level > 10 && player.country IN [\"US\", \"UK\"]")
```

Users not matching the rule get `DecisionResult.NoMatch`.
