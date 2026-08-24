---
name: authorization
description: Use when adding permission checks, or protecting a resource or endpoint so only the right users can access it.
---

# Authorization

- Enforced server-side, always — never trust a frontend check.
- Verify resource ownership and tenant boundaries on every request, not
  just at login.
- Default to deny when a permission can't be established.

Related skills in this plugin: `authentication` (establishing who the
user is, before deciding what they can do), `rate-limiting`.
