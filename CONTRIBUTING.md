# Contributing to Rulebook

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) — the exact version is
  pinned in [`global.json`](global.json)
- An editor that honours `.editorconfig`

## Setup

```bash
git clone https://github.com/deniscuciuc/rulebook.git
cd rulebook
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

Nothing external is required. `tests/Rulebook.Benchmarks` is a BenchmarkDotNet project; run
it with `dotnet run -c Release --project tests/Rulebook.Benchmarks`.

## Code style

Enforced by the build and by `dotnet format --verify-no-changes` in CI:

- `net10.0`, `LangVersion latest`, nullable reference types and implicit usings on
- `TreatWarningsAsErrors`, .NET analyzers at `Recommended`
- File-scoped namespaces; one namespace per project, matching the project name
- XML doc comments on public types and members
- `ArgumentNullException.ThrowIfNull` on every public entry point (CA1062 enforces this)
- `ConfigureAwait(false)` on every await in `src/` (CA2007 enforces this)
- **`CultureInfo.InvariantCulture` on every format and parse** (CA1305 and CA1310 enforce
  this). A rule compares values; a locale-dependent parse would change which side of a
  numeric threshold a value falls on, differently on different machines.

Deliberate deviations are listed in [`.editorconfig`](.editorconfig), each with the reason.

## Architecture

`Rulebook.Abstractions` holds the contracts and the definition models and depends on
nothing. `Rulebook` holds the engine: tokenizer, parser, compiler, operator registry,
contexts, evaluators and definition sources.

Rules are parsed to an immutable `RuleExpression` tree, then compiled to a delegate and
cached. Evaluation walks the tree with short-circuiting and resolves each path from the
`IDecisionContext`.

Nothing about the shape of a caller's data is baked in — paths are resolved dynamically, and
operators come from a registry that callers can extend.

## Adding an operator

1. Implement `IOperator` in `src/Rulebook/Operators/`.
2. Register it in `OperatorRegistry`'s built-in set if it belongs there, or leave it for
   callers to register through `AddDecisions(operators => ...)`.
3. Add cases to `tests/Rulebook.UnitTests/OperatorTests.cs`, including the mismatched-type
   and null-operand cases — an operator that throws on unexpected input turns a false
   decision into a 500.
4. Document it in the operator table in `docs/expressions.md`.

## Changing the expression language

Treat the grammar as public API. A change that makes an existing rule parse differently, or
stop parsing, is a **breaking change** even though no C# signature moved — someone's
`appsettings.json` is the caller. Say so in `CHANGELOG.md`.

## Documentation

Any C# you put in `README.md` or `docs/` must also exist in
`tests/Rulebook.Docs.Snippets`, which compiles in CI. A documented snippet that does not
compile is a bug, and this is what catches it.

## Pull requests

- Keep a PR to a single concern.
- New behaviour and bug fixes need tests.
- Conventional commits: `feat(tables): …`, `fix(pdf): …`, `docs(readme): …`.
- Note breaking changes in `CHANGELOG.md` under `## [Unreleased]`.
- CI must be green: format, build and test on Linux and Windows, and the pack assertion.

## Releasing

See [docs/release-process.md](docs/release-process.md). Releases are tag-driven; only
maintainers can approve the publish step.
