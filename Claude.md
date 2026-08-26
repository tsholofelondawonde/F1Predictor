# F1 Race Predictor

A Clean Architecture .NET solution that predicts F1 race outcomes from OpenF1 data — two
ML.NET binary classifiers (podium, points-finish) trained on engineered per-driver features,
served over a minimal API with a Next.js frontend. It also looks forwards: next-Grand-Prix
previews, live championship tables, and a Monte Carlo season simulator for title odds.

## Detailed Rules

@.claude/rules/architecture.md
@.claude/rules/conventions.md
@.claude/rules/ml-pipeline.md
@.claude/rules/api-conventions.md
@.claude/rules/setup.md
@.claude/rules/project-status.md

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- For broad navigation, start at graphify-out/wiki/index.md — 116 articles, one per community plus one per god node — instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
- `graphify update .` refreshes graph.json, GRAPH_REPORT.md and the labels, but **not** the wiki. To refresh the wiki, first delete graphify-out/.graphify_analysis.json, then run `graphify export wiki`. The delete is required: `update` leaves that sidecar stale and the wiki export prefers it over the current graph, so skipping the delete rebuilds the wiki from the old community assignment. Without the sidecar the export falls back to the graph itself and is correct, but drops the per-community cohesion scores; `graphify cluster-only .` regenerates the sidecar in full if you want them back.
