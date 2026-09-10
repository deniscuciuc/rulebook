![Rulebook](https://raw.githubusercontent.com/deniscuciuc/rulebook/main/assets/banner.png)

# Rulebook

[![CI](https://github.com/deniscuciuc/rulebook/actions/workflows/ci.yml/badge.svg)](https://github.com/deniscuciuc/rulebook/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Rulebook.svg?label=Rulebook)](https://www.nuget.org/packages/Rulebook/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

> Rules, feature flags, segments and A/B tests in .NET — from one expression language.

Write the condition once, as a string:

```
user.tier == 'gold' && cart.total > 100
```

Rulebook parses it, compiles it to a delegate, and evaluates it against whatever context you
hand it. Feature flags, audience segments and A/B experiments are all the same rule with a
different wrapper.

```
dotnet add package Rulebook
```

```csharp
builder.Services.AddDecisions();
builder.Services.AddDecisionDefinitions(builder.Configuration);
```

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
    ]
  }
}
```

```csharp
public sealed class CheckoutService(
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
```

Change the rule in configuration and the next evaluation uses it — no restart, no deploy.
A rule that does not parse throws on first use and names the definition, rather than
quietly evaluating to false.

## What you get

| | |
|---|---|
| **Rules** | A small boolean DSL — `==` `!=` `>` `<` `>=` `<=` `in` `not in` `contains` `starts_with` `ends_with`, `&&`, `\|\|`, `!`, parentheses. Parsed to an AST, compiled to a delegate, cached. |
| **Feature flags** | A rule plus a rollout percentage. |
| **Segments** | A named rule. Ask which segments a subject falls into. |
| **A/B tests** | Deterministic bucketing — `XxHash32("{experimentId}:{subjectId}")`, weighted variants, optional salt. The same subject always lands in the same variant, with no stored state and no coordination between instances. |
| **Custom operators** | Register your own; the registry is open. |

## Context

A decision is evaluated against an `IDecisionContext`, which resolves dotted paths. Nothing
about the shape of your data is baked in.

```csharp
var context = new DictionaryDecisionContext(new Dictionary<string, object?>
{
    ["player.level"] = 25,
    ["player.country"] = "US",
    ["subject.id"] = "user-42",
});
```

There is a synchronous fast path (`TryGetValue`) and an async fallback (`GetValueAsync`) for
values that have to be fetched. `CompositeDecisionContext` layers several sources.

## Definitions from anywhere

`AddDecisionDefinitions(IConfiguration)` reads from configuration through `IOptionsMonitor`,
so any reloadable source gives hot reload. To load from a database or a feature-flag service
instead, implement `IDecisionDefinitionProvider` and register it:

```csharp
services.AddDecisionDefinitionProvider<MyDefinitionProvider>();
```

Registration is `TryAdd`, so yours wins over the built-in one regardless of order.

## Packages

| Package | What you get |
|---|---|
| [`Rulebook`](https://www.nuget.org/packages/Rulebook/) | The engine: parser, compiler, operators, contexts, evaluators, definition sources |
| [`Rulebook.Abstractions`](https://www.nuget.org/packages/Rulebook.Abstractions/) | Contracts and definition models, with no dependencies. Reference this from a project that describes decisions without evaluating them. |

## Documentation

- [Getting started](docs/getting-started.md)
- [Expressions](docs/expressions.md) — the rule language
- [Context](docs/context.md)
- [Feature flags](docs/feature-flags.md)
- [Segmentation](docs/segmentation.md)
- [A/B testing](docs/ab-testing.md)
- [Extensibility](docs/extensibility.md) — custom operators and definition sources
- [Architecture](docs/architecture.md)
- [Release process](docs/release-process.md)

A runnable example lives in [`examples/Rulebook.Examples.Console`](examples/Rulebook.Examples.Console),
and there are BenchmarkDotNet suites in [`tests/Rulebook.Benchmarks`](tests/Rulebook.Benchmarks).

## Versioning

[Semantic versioning](https://semver.org/). Both packages are versioned and released
together. Breaking changes are listed in [CHANGELOG.md](CHANGELOG.md).

Targets **net10.0**. Building from source needs the **.NET 10 SDK**.

## Contributing

Issues and pull requests are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md),
[CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) and [SECURITY.md](SECURITY.md).

## License

[MIT](LICENSE)
