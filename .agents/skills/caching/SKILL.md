---
name: caching
description: Use when adding caching for an API response, database query, or computed result.
---

# Caching

Before adding any cache, answer: what's being cached, who can receive it,
how long it's valid, and how/when it's invalidated. If you can't answer
all four, don't add it yet.

- Never cache anything user-specific in a way another user could receive
  it — this includes auth responses, profiles, financial/personal data,
  authorization decisions, tenant-specific data.
- Document key, TTL, scope, invalidation trigger, and stale-data behavior
  for anything non-trivial.
