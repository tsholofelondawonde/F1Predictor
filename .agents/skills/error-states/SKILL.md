---
name: error-states
description: Use when handling API/network error responses in the UI, or confirming a successful user action (success/thank-you state).
---

# Error & Success States

## API errors
Handle these distinctly, not as one generic "something went wrong":
400, 401, 403, 404, 409, 429, 500, network failure, timeout.
Never surface a raw backend exception to the user.

## Success state
- Confirm what happened and what's next.
- Don't echo back more of the submitted data than necessary.
- If it's a routable page, give it its own metadata; set robots/noindex if
  it shouldn't be indexed.
