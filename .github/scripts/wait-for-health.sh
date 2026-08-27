#!/usr/bin/env bash
#
# Poll the container app's /health until it reports Healthy for the expected build.
#
# Why poll: minReplicas is 0, so the first request after an image swap has to cold-start a
# replica and can take tens of seconds. A single curl would flake.
#
# Why assert the build and not just the status: `az containerapp update` returns once the update
# is *accepted*. If the new revision then fails to provision, the container app keeps the previous
# revision serving — and a status-only check reads that as a successful deploy of a commit that
# never ran. The build field is stamped into the image by the Dockerfile's GIT_SHA build arg.
#
# Usage: APP_URL=https://... wait-for-health.sh [expected-sha]
#
# An empty expected-sha accepts any Healthy response. That is for rolling back to an image built
# before /health carried a build field, whose payload has no SHA worth asserting.

set -uo pipefail

expected="${1:-}"
attempts="${HEALTH_ATTEMPTS:-20}"
interval="${HEALTH_INTERVAL:-15}"

: "${APP_URL:?APP_URL must be set}"

if [ -n "$expected" ]; then
  echo "Waiting for $APP_URL/health to report Healthy on build $expected."
else
  echo "Waiting for $APP_URL/health to report Healthy (any build)."
fi

for attempt in $(seq 1 "$attempts"); do
  body=$(curl -fsS --max-time 30 "$APP_URL/health" 2>/dev/null || true)

  if [ -n "$body" ]; then
    echo "$body"
    case "$body" in
      *'"status":"Healthy"'*)
        if [ -z "$expected" ]; then
          echo "Healthy after $attempt attempt(s)."
          exit 0
        fi
        case "$body" in
          *"\"build\":\"$expected\""*)
            echo "Healthy on build $expected after $attempt attempt(s)."
            exit 0
            ;;
          *)
            # Healthy, but it is not the revision that was just deployed answering. Almost always
            # means the new revision failed to provision and traffic stayed on the previous one.
            echo "Attempt $attempt: Healthy, but NOT build $expected — an older revision is still serving."
            ;;
        esac
        ;;
      *)
        echo "Attempt $attempt: responded, but not Healthy."
        ;;
    esac
  else
    echo "Attempt $attempt: no response yet."
  fi

  if [ "$attempt" -lt "$attempts" ]; then
    sleep "$interval"
  fi
done

echo "Gave up after $attempts attempt(s) over roughly $((attempts * interval))s."
exit 1
