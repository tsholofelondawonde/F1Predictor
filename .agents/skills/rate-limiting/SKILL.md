---
name: rate-limiting
description: Use when exposing a public endpoint — especially login, registration, password reset, token refresh, public forms, search, or any expensive/AI-backed operation.
---

# Rate Limiting

- Cover login, registration, password reset, token refresh, email
  verification, public forms, search, and any expensive or AI-backed
  endpoint.
- Enforced server-side — frontend throttling is a suggestion, not a
  limit. Return 429 when exceeded.
