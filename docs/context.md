# Context System

## Overview

`IDecisionContext` is the abstraction used by the engine to resolve values at evaluation time. No fields are hardcoded — any domain can provide any data.

## Interface

```csharp
public interface IDecisionContext
{
    bool TryGetValue(string path, out object? value);
    ValueTask<object?> GetValueAsync(string path, CancellationToken ct = default);
}
```

- `TryGetValue` — sync fast path, used first by the engine
- `GetValueAsync` — async fallback when sync lookup returns false

## Built-in Implementations

### DictionaryDecisionContext

Wraps `IReadOnlyDictionary<string, object?>`. Supports:

- Direct key lookup: `"player.level"` as a flat key
- Dot-notation resolution: navigates nested dictionaries

```csharp
var ctx = new DictionaryDecisionContext(new Dictionary<string, object?>
{
    ["player"] = new Dictionary<string, object?>
    {
        ["level"] = 42,
        ["country"] = "US"
    },
    ["session.days"] = 7  // flat key also works
});
```

Resolution priority:
1. Direct key match (e.g., `"player.level"` as literal key)
2. Dot-path navigation (e.g., `"player"` → `"level"` through nested dict)

### CompositeDecisionContext

Chains multiple contexts. First match wins.

```csharp
var composite = new CompositeDecisionContext(
    userContext,      // checked first
    sessionContext,   // checked second
    systemContext     // checked last
);
```

## Custom Context

Implement `IDecisionContext` for custom resolution logic:

```csharp
public class GamePlayerContext : IDecisionContext
{
    private readonly Player _player;

    public bool TryGetValue(string path, out object? value)
    {
        value = path switch
        {
            "player.level" => _player.Level,
            "player.country" => _player.Country,
            "player.spent" => _player.TotalSpent,
            "subject.id" => _player.Id,
            _ => null
        };
        return value is not null;
    }

    public ValueTask<object?> GetValueAsync(string path, CancellationToken ct)
    {
        TryGetValue(path, out var value);
        return ValueTask.FromResult(value);
    }
}
```

## Reserved Paths

| Path | Purpose |
|------|---------|
| `subject.id` | User/entity identifier for A/B bucketing and feature flag rollout |
