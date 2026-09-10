# Architecture

## Overview

Rulebook is a generic, deterministic decision engine for .NET. It evaluates rules dynamically at runtime with zero infrastructure dependencies — no databases, no HTTP, no Redis. Pure in-process evaluation.

## Core Pipeline

```
┌─────────────────────┐
│  Definition Source   │  Configuration, code, or a source of your own
│  (external)          │  Returns DecisionDefinition / SegmentDefinition / etc.
└─────────┬───────────┘
          ▼
┌─────────────────────┐
│  Expression Parser   │  String → RuleExpression AST
│  Recursive descent   │  Cached (ConcurrentDictionary)
└─────────┬───────────┘
          ▼
┌─────────────────────┐
│  Decision Engine     │  Central evaluator
│  - rule evaluation   │  Walks expression tree
│  - operator dispatch │  Resolves operators from registry
│  - context lookup    │  Resolves values from IDecisionContext
│  - variant selection │  A/B bucket assignment + weight mapping
└─────────┬───────────┘
          ▼
┌─────────────────────┐
│  DecisionResult      │  IsMatch, VariantId?, Payload?
└─────────────────────┘
```

## Package Dependency Graph

```
Rulebook.Abstractions        ← Zero dependencies (interfaces, models, records)
    ↑
Rulebook                     ← Core engine, parser, operators, context, definition sources
```

Two packages, and the second is the only one that takes a dependency on anything —
`Microsoft.Extensions.*` for DI and options, and `System.IO.Hashing` for bucketing.

## Key Concepts

### RuleExpression (AST)

All rules are represented as an immutable expression tree:

- **ConditionExpression** — `path OPERATOR value` (e.g., `player.level > 10`)
- **GroupExpression** — `AND` / `OR` with N children (flattened, not nested pairs)
- **NotExpression** — negation wrapper

### IDecisionContext

Dynamic value resolution. No hardcoded fields. Supports:
- Sync fast path: `TryGetValue("player.level", out var value)`
- Async fallback: `GetValueAsync("player.level")` for lazy/computed values
- Dot-notation path resolution through nested dictionaries

### Operator System

Registry-based. Built-in operators: `==`, `!=`, `>`, `<`, `>=`, `<=`, `IN`, `NOT_IN`, `CONTAINS`, `STARTS_WITH`, `ENDS_WITH`. Custom operators can be registered at startup.

### Deterministic A/B Testing

Uses XxHash32 of `"{experimentId}:{subjectId}"` → `hash % totalBuckets`. Same inputs always produce the same bucket. No external state required.

## Evaluation Flow

1. **Parse** — String expression → `RuleExpression` AST (cached)
2. **Walk** — Recursive tree traversal with short-circuit (AND stops on first false, OR stops on first true)
3. **Resolve** — Each condition resolves its path from `IDecisionContext`
4. **Compare** — Operator from registry evaluates `(leftValue, rightValue)` → bool
5. **Result** — For segments/flags: `IsMatch`. For experiments: `IsMatch + VariantId + Payload`

## Integration Flow

```
Configuration (or your own source) holds rules as expression strings
    ↓
IDecisionDefinitionProvider parses them once per configuration revision
    ↓
Request arrives
    ↓
Consumer builds IDecisionContext from user/session/system data
    ↓
DecisionEngine.EvaluateAsync(definition, context)
    ↓
DecisionResult → system acts on it
```

The parse happens once per configuration revision, not per evaluation: the provider caches
the compiled set and only rebuilds when `IOptionsMonitor` hands it a new instance.
