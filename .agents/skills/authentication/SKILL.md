---
name: authentication
description: Use when creating or modifying login, signup, sessions, or authentication error handling.
---

# Authentication

- Auth error messages must never enable account enumeration — "user does
  not exist", "incorrect password", "email belongs to another account"
  are all leaks. Use generic failure messages.
- This is high-risk work: review the whole auth flow, not just the diff.

Related skills in this plugin: `token-security` (issuing/storing the
tokens this flow produces), `authorization` (what a logged-in user can
then do), `rate-limiting` (login/reset endpoints need it). If you're
building a full login/signup flow, consult those too — don't treat this
skill as the complete auth implementation.
