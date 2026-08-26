---
name: token-security
description: Use when implementing or reviewing how access/refresh tokens are issued, stored, or rotated.
---

# Token Security

- Access tokens: short-lived, minimum required permissions, validated
  server-side, rejected once expired.
- Refresh tokens: cryptographically random, stored server-side or in a
  secure token store, revocable, rotated on use, reuse-detected where
  practical.
- For browser apps, prefer HttpOnly + Secure + SameSite cookies for
  refresh tokens over exposing them to JS. Avoid localStorage/
  sessionStorage/IndexedDB for auth tokens unless there's a specific,
  reviewed reason.

Related skills in this plugin: `authentication`, `secrets-management`.
