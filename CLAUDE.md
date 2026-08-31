# F1 Race Predictor

A Clean Architecture .NET solution that predicts F1 race outcomes from OpenF1 data — two
ML.NET binary classifiers (podium, points-finish) trained on engineered per-driver features,
served over a minimal API with a Next.js frontend (the "GridMind" product front end). It
also looks forwards: next-Grand-Prix previews, live championship tables, and a Monte Carlo
season simulator for title odds.

> Naming: the solution, namespaces, and repo are **F1Predictor**; the user-facing web app is
> branded **GridMind**. Both names are intentional.

## Detailed Rules

@.claude/rules/architecture.md
@.claude/rules/conventions.md
@.claude/rules/ml-pipeline.md
@.claude/rules/api-conventions.md
@.claude/rules/setup.md
@.claude/rules/project-status.md

## Optional: graphify knowledge graph

If a contributor runs [graphify](https://github.com/anthropics/graphify) locally
(`graphify .`), it produces a `graphify-out/` knowledge graph — god nodes, community
structure, cross-file relationships — that is useful for codebase questions and navigation.
`graphify-out/` is gitignored; there is no committed graph. Copy `.mcp.json.example` to
`.mcp.json` to expose it over MCP.
