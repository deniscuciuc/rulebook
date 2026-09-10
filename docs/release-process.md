# Release process

Releases are tag-driven. Pushing a `v*.*.*` tag builds, tests, packs, creates the GitHub
release and publishes to nuget.org. There is no manual `dotnet nuget push` and no long-lived
API key.

## Versioning

The version comes from the git tag via [MinVer](https://github.com/adamralph/minver) — no
`<Version>` element exists anywhere in the repository. A build from an untagged commit
produces a prerelease version derived from the last tag.

Both packages are versioned and released together, so a consumer never has to reason about
which combination of `Rulebook` and `Rulebook.Abstractions` is compatible.

[Semantic versioning](https://semver.org/) applies to the public API: `IDecisionEngine`,
`IDecisionContext`, `IDecisionDefinitionProvider`, `IOperatorRegistry`, the definition models
— **and the expression language itself**. A change that makes an existing rule parse
differently, or stop parsing, is a breaking change even though no C# signature moved.

## Cutting a release

1. Make sure `main` is green.
2. Move the `## [Unreleased]` entries in [`CHANGELOG.md`](../CHANGELOG.md) into a new version
   section with today's date, and update the link definitions at the bottom. The release
   workflow reads that section for the release notes and **fails if it is missing**.
3. Commit, tag and push:

   ```bash
   git tag -a v1.1.0 -m "v1.1.0"
   git push origin main --follow-tags
   ```

4. The `Release` workflow runs. It packs, uploads the `.nupkg` and `.snupkg` files, creates
   the GitHub release, and then waits on the `nuget` environment before publishing.
5. Approve the `nuget` environment. Publishing to nuget.org is irreversible — a version can
   be unlisted but never replaced — so it sits behind a manual gate.

## Trusted Publishing

The workflow authenticates to nuget.org with [Trusted
Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing) (OIDC). There is
no `NUGET_API_KEY` secret to rotate or leak.

Setup, once per package prefix:

1. On nuget.org, go to your account then **Trusted Publishing**.
2. Add a policy for the `Rulebook` package prefix, owner `deniscuciuc`, repository
   `deniscuciuc/rulebook`, workflow `release.yml`, environment `nuget`.
3. Set the repository variable `NUGET_USERNAME` to your nuget.org username.

## Local verification before tagging

```bash
dotnet build -c Release
dotnet test -c Release
dotnet pack -c Release -o ./artifacts
ls ./artifacts   # expect exactly 2 .nupkg and 2 .snupkg, nothing else
```

The package count is the check that matters: the console example, the benchmarks or a test
project accidentally becoming packable shows up here and nowhere else.
