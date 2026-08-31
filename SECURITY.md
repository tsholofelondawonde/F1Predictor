# Security Policy

## Reporting a vulnerability

Please **do not open a public issue** for security problems.

Use GitHub's private vulnerability reporting: go to the repository's **Security** tab →
**Report a vulnerability**. That opens a private advisory visible only to the maintainers.

Include what you can: affected component, a reproduction, and the impact you see. You'll get
an acknowledgement within a few days.

## Scope notes

A few things are known and intentional, so they are not vulnerabilities:

- **The `X-Api-Key` on the ingest / rebuild / train / import endpoints is a low bar, not
  access control.** The same key ships in the public frontend bundle
  (`NEXT_PUBLIC_API_KEY`). It filters naive automated hits against the raw API; it does not
  stop a determined caller. Anything that must be genuinely restricted should sit behind a
  real identity provider — that work is not done here.
- **`POST /api/admin/import-legacy-sqlite` is Development-only** and reads a local file path.
  It is not mapped outside the Development environment.
- The app makes **unauthenticated outbound calls to the OpenF1 API** only.

Reports about the deployment pipeline (Azure OIDC federation, container-app secrets) are in
scope and welcome.
