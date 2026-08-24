---
name: forms-ux
description: Standards for building or modifying forms, submission flows, and user-facing async actions (search, filtering, uploads). Use whenever creating a form, adding validation, wiring up an API request the user waits on, or handling submission success/failure — even if the request just says "add a signup form" or "hook this button to the API" without using the word "form."
---

# Forms, Validation & Async UX

## Validation — both layers, not one
- Frontend: validate required fields and formats for immediate feedback;
  block obviously invalid submits.
- Backend: re-validate everything the frontend validated, plus business
  rules, regardless of what the client sent. Frontend validation is UX;
  backend validation is the actual guarantee.
- Show errors near the relevant field, in plain language, without
  discarding the user's valid input.

## Loading states
- Every async action the user is waiting on (submit, upload, search, AI
  call) needs a state that shows work is happening.
- Prevent duplicate submissions (disable the control, not just show a
  spinner next to it).
- Prefer a contextual loading state (inline, per-field, per-button) over a
  global spinner unless the whole view is genuinely blocked.
- No unnecessary layout shift when the state changes.

## API error handling
Handle these distinctly, not as one generic "something went wrong":
400, 401, 403, 404, 409, 429, 500, network failure, timeout.
Never surface a raw backend exception to the user.

## Success state
- Confirm what happened and what's next.
- Don't echo back more of the submitted data than necessary.
- If it's a routable page, give it its own metadata; set robots/noindex if
  it shouldn't be indexed.

## Accessibility
Real labels on every control, keyboard-navigable, visible focus states, no
interaction that only works on hover.