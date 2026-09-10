# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-09-11

First public release. Extracted from a private monorepo, relicensed under MIT, and renamed
from `QGCore.Decisions`.

### Breaking

- Every namespace, package and assembly renamed from `QGCore.Decisions.*` to `Rulebook.*`.
- `IOperatorRegistry.Get(string)` is now `Resolve(string)`. `Get` is a reserved word in
  Visual Basic, which made the interface awkward to implement from other languages
  (CA1716) — and `Resolve` is the better name for a registry lookup anyway.
- `IExpressionParser.TryParse`'s third parameter is now `errorMessage` rather than `error`,
  for the same reason.
- `QGCore.Decisions.LiveConfig` is gone. It bound decision definitions to one specific
  private configuration framework; **the capability it provided is now in the core package
  and works with any configuration source** — see below.

### Added

- **Definitions from configuration.** `AddDecisionDefinitions(IConfiguration)` binds
  decisions, segments, feature flags and experiments from the `Rulebook` section, with each
  rule written as an expression string:

  ```json
  { "Rulebook": { "FeatureFlags": [
      { "Id": "new-checkout", "Rule": "user.tier == 'gold'", "RolloutPercentage": 25 }] } }
  ```

  It reads through `IOptionsMonitor`, so any reloadable configuration source gives hot
  reload with no extra code. Expressions are parsed once per configuration revision and
  cached, so a steady-state lookup is a frozen-dictionary probe. A rule that does not parse
  throws on first use and names the definition, rather than silently evaluating to false.
- `AddDecisionDefinitionProvider<T>()`, for loading definitions from a database, a
  feature-flag service, or a configuration framework of your own. Registration is `TryAdd`,
  so a custom provider wins over the built-in one regardless of registration order.
- MIT license. The source repository claimed PolyForm Strict — source-available, not open
  source — and shipped no `LICENSE` file at all.
- 12 tests covering the definition layer: configuration binding, rule parsing, the caching
  path, custom providers, and the two failure modes (a rule that does not parse, and a
  definition with no rule at all).
- Package metadata on both packages: description, tags, per-package README, symbol packages,
  SourceLink and deterministic CI builds. MinVer tag-based versioning replaces a hardcoded
  `<Version>`.
- .NET analyzers at `Recommended` with `TreatWarningsAsErrors`, and an `.editorconfig` that
  explains every deliberately loosened rule.
- `tests/Rulebook.Docs.Snippets`, which compiles every code sample in `README.md` and
  `docs/` so a documented snippet cannot silently rot.
- CI on Linux and Windows with coverage, a pack job that asserts the exact set of two
  packages, CodeQL, gitleaks, Dependabot, `SECURITY.md`, `CODEOWNERS` and templates.
- Release via NuGet Trusted Publishing (OIDC) behind a manual environment gate — no
  long-lived API key.

### Fixed

- Public entry points on `DecisionEngine`, `SegmentEvaluator`, `VariantSelector`,
  `OperatorRegistry`, `BoundAccessor`, `CumulativeWeightTable` and
  `RuleExpressionJsonConverter` now validate their arguments instead of surfacing a
  `NullReferenceException` from inside the evaluator (CA1062).
- `ConfigureAwait(false)` is applied throughout `src/`, so evaluating a decision from a UI
  thread cannot deadlock (CA2007).
- Dependencies updated: `System.IO.Hashing` 9.0.5 → 10.0.12, `Microsoft.Extensions.*`
  10.0.0 → 10.0.5, `BenchmarkDotNet` 0.14.0 → 0.15.8, and the test stack to current.

### Removed

- Documentation describing publication to a private GitHub Packages feed.

[Unreleased]: https://github.com/deniscuciuc/rulebook/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/deniscuciuc/rulebook/releases/tag/v1.0.0
