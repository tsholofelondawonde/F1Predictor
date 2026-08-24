---
name: testing
description: Use when adding tests for authentication, validation, or rate-limited/routed behavior, or when asked what to test for a feature.
---

# Testing Coverage

At minimum, test:

- **Auth**: login success/failure, expired access token, refresh success/
  expiration/revocation, refresh reuse detection where implemented,
  unauthorized and forbidden requests.
- **Validation**: valid request accepted; missing required values,
  invalid formats, and boundary values rejected; unauthorized resource
  access rejected.
- **Rate limiting**: requests below the limit succeed; requests over it
  return 429.
- **Routing**: unknown routes show the custom 404.
- **Forms**: invalid forms show validation errors; valid forms submit;
  loading state shows during submission; failure shows an error; success
  shows the confirmation state.
