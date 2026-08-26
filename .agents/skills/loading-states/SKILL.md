---
name: loading-states
description: Use whenever wiring up an async action the user waits on — submit, upload, search, filtering, an AI call, or page-level data fetching.
---

# Loading States

- Every async action the user is waiting on needs a state that shows work
  is happening.
- Prevent duplicate submissions (disable the control, not just show a
  spinner next to it).
- Prefer a contextual loading state (inline, per-field, per-button) over a
  global spinner unless the whole view is genuinely blocked.
- No unnecessary layout shift when the state changes.
