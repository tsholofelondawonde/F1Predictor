---
name: form-validation
description: Use when building or modifying a form and its validation logic.
---

# Form Validation

- Frontend: validate required fields and formats for immediate feedback;
  block obviously invalid submits.
- Backend: re-validate everything the frontend validated, plus business
  rules, regardless of what the client sent. Frontend validation is UX;
  backend validation is the actual guarantee — never trust the client-side
  pass alone.
- Show errors near the relevant field, in plain language, without
  discarding the user's valid input.
