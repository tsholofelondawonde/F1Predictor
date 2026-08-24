---
name: csrf
description: Use when authentication relies on cookies, or when reviewing form/session security against cross-site request forgery.
---

# CSRF

- Don't assume cookie-based auth is automatically safe or unsafe —
  actually evaluate the exposure.
- Check SameSite cookie configuration, anti-CSRF tokens, and
  origin/referer validation.

Related skills in this plugin: `cors` (the two are usually reviewed
together), `authentication`.
