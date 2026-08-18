---
name: pages-seo
description: Standards for creating a new page or route — metadata, Open Graph, 404 handling, and responsive layout. Use whenever adding a new route, page, or view, or when the request touches SEO, sharing previews, or how something looks on mobile/tablet.
---

# Pages, Metadata & Responsiveness

## 404 / not-found
Use the framework's native not-found mechanism if it has one. Visually
consistent with the app, clear "not found" message, a way back home,
responsive, no leaked routing internals.

## Meta title & description
Every public page gets a unique title and description that actually
describe its content — no copy-pasted defaults across pages, no keyword
stuffing. Use the framework's metadata API rather than hand-rolled tags
where one exists.

## Open Graph
- Minimum: og:title, og:description, og:image, og:url, og:type —
  page-specific where it matters.
- Default OG image for the app; override per-page where supported.
- OG image URL must be absolute and publicly reachable — never a localhost
  URL in production metadata.

## Responsive layout
- Check the existing design system/breakpoints before adding new ones —
  don't invent a parallel set.
- Must hold up at mobile, tablet, and desktop; no horizontal overflow
  unless intentional; nav and forms stay usable on small screens and touch
  targets stay tappable.