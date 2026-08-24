---
name: security-headers
description: Use when configuring transport/security headers, or preparing an application for production.
---

# Security Headers & Transport

- Review CSP, HSTS, X-Content-Type-Options, Referrer-Policy,
  Permissions-Policy, and frame protection against what the app actually
  loads (analytics, fonts, payment providers, embeds) so you don't break
  them.
- HTTPS enforced in production, HSTS where appropriate, no mixed content.
