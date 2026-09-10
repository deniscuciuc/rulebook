# Extensibility

## Custom Operators

Implement `IOperator` and register at startup:

```csharp
public class RegexMatchOperator : IOperator
{
    public string Name => "REGEX";

    public bool Evaluate(object? left, object? right)
    {
        if (left is not string input || right is not string pattern)
            return false;

        return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);
    }
}

// Register
services.AddDecisions(operators =>
{
    operators.Register(new RegexMatchOperator());
});

// Use in expressions
// player.email REGEX "^admin@.*\\.com$"
```

## Custom Context Providers

Implement `IDecisionContext` for domain-specific value resolution:

```csharp
public class GameSessionContext : IDecisionContext
{
    private readonly GameSession _session;

    public bool TryGetValue(string path, out object? value)
    {
        // Fast sync resolution from in-memory game state
        value = path switch
        {
            "session.duration" => _session.Duration.TotalMinutes,
            "session.matchesPlayed" => _session.MatchCount,
            _ => null
        };
        return value is not null;
    }

    public async ValueTask<object?> GetValueAsync(string path, CancellationToken ct)
    {
        // Async resolution for computed/external values
        if (path == "session.winRate")
            return await _session.CalculateWinRateAsync(ct);

        TryGetValue(path, out var value);
        return value;
    }
}
```

## Composite Contexts

Layer multiple contexts:

```csharp
var context = new CompositeDecisionContext(
    new GameSessionContext(session),    // checked 1st
    new PlayerContext(player),          // checked 2nd
    new SystemContext()                 // checked 3rd
);
```

## Custom Definition Sources

Implement `IDecisionDefinitionProvider`:

```csharp
public class ApiDecisionProvider : IDecisionDefinitionProvider
{
    // Load definitions from your API, database, etc.
}
```

## JSON Serialization

Use `DecisionJsonOptions.Create()` for JSON round-tripping of `RuleExpression`:

```csharp
var options = DecisionJsonOptions.Create();

// Serialize
var json = JsonSerializer.Serialize(ruleExpression, options);

// Deserialize
var expr = JsonSerializer.Deserialize<RuleExpression>(json, options);
```

JSON format:
```json
{
  "type": "group",
  "op": "AND",
  "children": [
    { "type": "condition", "path": "player.level", "op": ">", "value": 10 },
    { "type": "condition", "path": "player.country", "op": "==", "value": "US" }
  ]
}
```
