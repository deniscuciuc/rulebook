# Security Policy

## Reporting a Vulnerability

**Please do not open a public issue for a security vulnerability.**

Report it through GitHub's private vulnerability reporting, which is the preferred channel:

<https://github.com/deniscuciuc/rulebook/security/advisories/new>

If you cannot use GitHub, email **denis@deniscuciuc.dev** instead.

Please include a description of the vulnerability and its impact, steps to reproduce or a
proof of concept, the affected module, and any suggested mitigation.

## What to Expect

| Stage | Target |
|---|---|
| Acknowledgement of your report | Within 48 hours |
| Initial assessment and severity triage | Within 5 working days |
| Fix released for a high or critical issue | Within 30 days of triage |
| Fix released for a moderate or low issue | Next scheduled release |

If you have not heard back within 48 hours, please follow up — an unanswered report usually
means it did not arrive.

## Supported Versions

| Version | Supported |
|---|---|
| 1.x     | Yes |
| < 1.0   | No  |

Only the latest patch of the latest minor release receives security fixes. All Rulebook
packages are versioned and released together, so a fix ships as a new patch across all of
them.

## Scope

In scope: the Rulebook packages in this repository. The highest-value target is the
expression parser and compiler — anything where a rule expression, which is often authored
by someone with less trust than the person who deployed the service, could cause unbounded
recursion, unbounded memory use, or evaluation that escapes the decision context.

Also in scope: any way a decision could be made to return the wrong answer for a given
subject, since flags and experiments gate access to real functionality.

Out of scope: rules you author yourself being wrong.

## Dependency Advisories

The build does not suppress NuGet vulnerability warnings. `NuGetAudit` is on with
`NuGetAuditMode=all` and `NuGetAuditLevel=low`, and `TreatWarningsAsErrors` makes any
published advisory — including transitive ones — fail the build. Advisories are resolved by upgrading, or pinned in
[`Directory.Packages.props`](Directory.Packages.props) with the GHSA identifier in a comment.

Dependabot is enabled for NuGet and GitHub Actions, CodeQL runs on every push to `main` and
weekly, and gitleaks scans the full history on every push and pull request.
