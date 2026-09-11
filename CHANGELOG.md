# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.1] - 2026-09-11

### Changed

- Dependencies brought current: `Microsoft.Extensions.*` 10.0.12, `NSubstitute` 6.2.0,
  `Microsoft.NET.Test.Sdk` 18.10.0, `xunit.runner.visualstudio` 4.0.0,
  `coverlet.collector` 10.0.1.

## [1.0.0] - 2026-09-11

Initial release. A boolean expression DSL compiled to delegates, with feature flags,
segmentation and deterministic A/B bucketing built on top of it.

### Packages

`Rulebook` (the engine: parser, compiler, operator registry, contexts, evaluators, definition
sources) and `Rulebook.Abstractions` (contracts and definition models, no dependencies).

### Definitions from configuration

`AddDecisionDefinitions(IConfiguration)` binds decisions, segments, feature flags and
experiments from the `Rulebook` section, with each rule written as an expression string:

```json
{ "Rulebook": { "FeatureFlags": [
    { "Id": "new-checkout", "Rule": "user.tier == 'gold'", "RolloutPercentage": 25 }] } }
```

- Rules are held as expression strings rather than as object graphs, because `RuleExpression`
  is polymorphic and a configuration binder cannot construct it — and because an expression is
  the readable form in an `appsettings.json`.
- It reads through `IOptionsMonitor`, so **any** reloadable configuration source gives hot
  reload with no extra code.
- Expressions are parsed once per configuration revision and cached, so a steady-state lookup
  is a frozen-dictionary probe.
- A rule that does not parse **throws on first use and names the definition**, rather than
  silently evaluating to false.
- `AddDecisionDefinitionProvider<T>()` covers a database or feature-flag service instead. It
  is `TryAdd`, so a custom provider wins regardless of registration order.

### API notes

- `IOperatorRegistry.Resolve(string)` rather than `Get` — `Get` is a reserved word in Visual
  Basic (CA1716), and `Resolve` is the better name for a registry lookup.
- `IExpressionParser.TryParse`'s third parameter is `errorMessage`, for the same reason.

### Build, CI and release

- .NET analyzers at `Recommended` with `TreatWarningsAsErrors`; every deliberately loosened
  rule is explained in `.editorconfig`. `NuGetAudit` runs in `all` mode at `low` level.
- MinVer derives the version from the git tag. Symbol packages, SourceLink and deterministic
  CI builds; per-package README, description and tags.
- CI builds and tests on Linux and Windows with coverage, and a pack job asserts the exact
  set of two packages.
- `tests/Rulebook.Docs.Snippets` compiles every code sample in `README.md` and `docs/`.
- Releases are tag-driven and publish through NuGet Trusted Publishing (OIDC) behind a manual
  environment gate — no long-lived API key.
- CodeQL, gitleaks and Dependabot are enabled.

### Testing

108 tests, including 12 covering the definition layer: configuration binding, rule parsing,
the caching path, custom providers, and the two failure modes (a rule that does not parse,
and a definition with no rule at all).

[Unreleased]: https://github.com/deniscuciuc/rulebook/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/deniscuciuc/rulebook/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/deniscuciuc/rulebook/releases/tag/v1.0.0
