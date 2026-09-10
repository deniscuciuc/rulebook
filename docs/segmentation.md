# Segmentation

## Overview

Segments are named rule-based groups. A user either matches a segment or doesn't.

## Define a Segment

```csharp
var segment = new SegmentDefinition(
    Id: "whales",
    Name: "Whales",
    Rule: parser.Parse("player.spent > 100")
);
```

## Evaluate

```csharp
var result = await engine.EvaluateSegmentAsync(segment, context);
if (result.IsMatch)
{
    // User is in the "whales" segment
}
```

## Convenience Evaluator

```csharp
var segmentEval = provider.GetRequiredService<SegmentEvaluator>();

// Single segment
var isWhale = await segmentEval.IsMatchAsync(whaleSegment, context);

// Multiple segments — get all matching
var allSegments = new List<SegmentDefinition> { whales, newPlayers, vips };
var matched = await segmentEval.GetMatchingSegmentsAsync(allSegments, context);
```

## Segment Examples

```csharp
// High spenders
new SegmentDefinition("whales", "Whales",
    parser.Parse("player.spent > 100"))

// Inactive users
new SegmentDefinition("inactive", "Inactive",
    parser.Parse("session.daysSinceLastLogin > 30"))

// VIP US players
new SegmentDefinition("vip-us", "VIP US Players",
    parser.Parse("player.vip == true && player.country == \"US\""))

// New players from specific countries
new SegmentDefinition("new-eu", "New EU Players",
    parser.Parse("player.level < 5 && player.country IN [\"DE\", \"FR\", \"IT\"]"))
```

## LiveOps Integration

Segments are the building block for LiveOps targeting:

```csharp
public class SegmentedOfferService
{
    private readonly IDecisionEngine _engine;
    private readonly IDecisionDefinitionProvider _provider;

    public async Task<Offer?> GetOfferAsync(IDecisionContext context)
    {
        var segments = await _provider.GetSegmentsAsync();

        foreach (var segment in segments)
        {
            var result = await _engine.EvaluateSegmentAsync(segment, context);
            if (result.IsMatch)
                return GetOfferForSegment(segment.Id);
        }

        return null;
    }
}
```
