#!/usr/bin/env bash
#
# One-time provisioning of the API's Azure Container Apps deployment.
# Why each choice was made: .claude/rules/setup.md, "Deployment → Provisioning (one time)".
#
# Creates (or skips, if already present):
#   - Log Analytics workspace (30-day retention, 0.1 GB/day cap — inside the free tier)
#   - Azure Container Registry, Basic SKU, admin user disabled
#   - Container Apps environment, Consumption only
#   - The first image, built in the cloud by `az acr build` (no local Docker needed)
#   - The container app: 0.5 vCPU / 1 GiB, scale 0..1, pulling with its own managed identity
#   - An Entra app registration GitHub Actions signs in as, via OIDC (no client secret)
# and then writes the repo variables/secrets deploy.yml reads.
#
# Run it from your OWN terminal (Git Bash on Windows), not through an agent or a `!` prompt:
# it asks for the Neon connection string and API key with hidden input, which needs a TTY,
# and keeps them out of any transcript.
#
# Prerequisites: `az login`, `gh auth login`, run from a clone of the repo.
#
# Safe to re-run: every resource is looked up first and only created when missing. An
# existing container app is left untouched (secrets are only asked for when creating it).

set -euo pipefail

# ---- Settings ---------------------------------------------------------------------------

LOCATION="${LOCATION:-germanywestcentral}"   # next to Neon eu-central-1 (Frankfurt)
RG="${RG:-rg-grid-mind}"
LOG_WORKSPACE="${LOG_WORKSPACE:-log-grid-mind}"
ACA_ENV="${ACA_ENV:-cae-grid-mind}"
CONTAINER_APP="${CONTAINER_APP:-ca-grid-mind-api}"
ACR_PREFIX="${ACR_PREFIX:-gridmindacr}"       # a random suffix is added: ACR names are global
IMAGE_NAME="${IMAGE_NAME:-f1predictor-api}"
GH_REPO="${GH_REPO:-tsholofelondawonde/F1Predictor}"
GH_ENVIRONMENT="${GH_ENVIRONMENT:-azure-production}"
OIDC_APP_NAME="${OIDC_APP_NAME:-gh-f1predictor-deploy}"
FRONTEND_URL="${FRONTEND_URL:-http://localhost:3000}"   # the one CORS origin; set to the Vercel URL

# ---- Environment quirks ------------------------------------------------------------------

# Git Bash rewrites arguments that look like POSIX paths, which turns Azure resource IDs
# ("/subscriptions/...") into "C:/Program Files/Git/subscriptions/..." before az sees them.
export MSYS_NO_PATHCONV=1
# az acr build streams build logs; on a Windows console the default encoding chokes on them.
export PYTHONIOENCODING=utf-8
# On Windows, az ends every line with CRLF, so `$(az ... -o tsv)` would carry a stray \r into
# every ID and name captured below. Strip it everywhere; pipefail keeps az's exit code.
az() { command az "$@" | tr -d '\r'; }

cd "$(dirname "$0")/.."

step() { printf '\n==> %s\n' "$*"; }
die() { printf 'ERROR: %s\n' "$*" >&2; exit 1; }

# ---- Preflight ---------------------------------------------------------------------------

step "Checking prerequisites"
command -v az >/dev/null || die "Azure CLI not found."
command -v gh >/dev/null || die "GitHub CLI not found."
az account show >/dev/null 2>&1 || die "Not signed in to Azure. Run: az login"
gh auth status >/dev/null 2>&1 || die "Not signed in to GitHub. Run: gh auth login"
[ -t 0 ] || die "No terminal attached. Run this from your own terminal so secrets can be typed hidden."

SUBSCRIPTION_ID=$(az account show --query id -o tsv)
TENANT_ID=$(az account show --query tenantId -o tsv)
echo "Subscription: $(az account show --query name -o tsv) ($SUBSCRIPTION_ID)"

GIT_SHA=$(git rev-parse HEAD)
echo "First image will be built from commit $GIT_SHA (uncommitted changes are not included)."

az extension add --name containerapp --upgrade --only-show-errors

# ---- Resource providers ------------------------------------------------------------------

step "Registering resource providers (first time takes a few minutes)"
for ns in Microsoft.App Microsoft.ContainerRegistry Microsoft.OperationalInsights; do
  az provider register --namespace "$ns" --wait
  echo "$ns: $(az provider show -n "$ns" --query registrationState -o tsv)"
done

# ---- Resource group ----------------------------------------------------------------------

step "Resource group $RG"
if [ "$(az group exists -n "$RG")" = "true" ]; then
  echo "Exists."
else
  az group create -n "$RG" -l "$LOCATION" -o none
  echo "Created."
fi

# ---- Log Analytics -----------------------------------------------------------------------

step "Log Analytics workspace $LOG_WORKSPACE"
if az monitor log-analytics workspace show -g "$RG" -n "$LOG_WORKSPACE" -o none 2>/dev/null; then
  echo "Exists."
else
  az monitor log-analytics workspace create -g "$RG" -n "$LOG_WORKSPACE" -l "$LOCATION" \
    --retention-time 30 -o none
  echo "Created."
fi
# Daily ingestion cap in GB: a runaway logger can't run up a bill past the free tier.
az monitor log-analytics workspace update -g "$RG" -n "$LOG_WORKSPACE" --quota 0.1 -o none
LOG_CUSTOMER_ID=$(az monitor log-analytics workspace show -g "$RG" -n "$LOG_WORKSPACE" --query customerId -o tsv)
LOG_KEY=$(az monitor log-analytics workspace get-shared-keys -g "$RG" -n "$LOG_WORKSPACE" --query primarySharedKey -o tsv)

# ---- Container registry ------------------------------------------------------------------

step "Container registry"
ACR_NAME=$(az acr list -g "$RG" --query "[?starts_with(name, '$ACR_PREFIX')].name | [0]" -o tsv)
if [ -n "$ACR_NAME" ]; then
  echo "Exists: $ACR_NAME"
else
  ACR_NAME="${ACR_PREFIX}$(openssl rand -hex 3)"
  az acr create -g "$RG" -n "$ACR_NAME" -l "$LOCATION" --sku Basic --admin-enabled false -o none
  echo "Created: $ACR_NAME"
fi
ACR_LOGIN_SERVER=$(az acr show -n "$ACR_NAME" --query loginServer -o tsv)
ACR_ID=$(az acr show -n "$ACR_NAME" --query id -o tsv)

# ---- Container Apps environment ----------------------------------------------------------

step "Container Apps environment $ACA_ENV"
if az containerapp env show -g "$RG" -n "$ACA_ENV" -o none 2>/dev/null; then
  echo "Exists."
else
  # Consumption only: no workload profiles, so no dedicated-plan management fee.
  az containerapp env create -g "$RG" -n "$ACA_ENV" -l "$LOCATION" \
    --logs-workspace-id "$LOG_CUSTOMER_ID" --logs-workspace-key "$LOG_KEY" \
    --enable-workload-profiles false -o none
  echo "Created."
fi

# ---- Container app -----------------------------------------------------------------------

step "Container app $CONTAINER_APP"
if az containerapp show -g "$RG" -n "$CONTAINER_APP" -o none 2>/dev/null; then
  echo "Exists — leaving its image, secrets and settings alone. deploy.yml swaps images from here on."
  EXPECTED_BUILD=""
else
  echo "Paste the Neon PRODUCTION (main branch) connection string, then press Enter."
  echo "Either form works: postgresql://… or Host=…;Database=…;Username=…;Password=…"
  echo "(Input is hidden. In Git Bash paste with Shift+Insert or right-click → Paste.)"
  read -rs NEON_CONNECTION_STRING; echo
  NEON_RAW_LENGTH=${#NEON_CONNECTION_STRING}
  # A paste can bring along more than the string: control characters (CR, bracketed-paste
  # markers like ESC[200~), quotes, or a whole snippet such as `psql '…'` or
  # `DATABASE_URL=…`. Keep printable characters and pull out just the connection string.
  NEON_CONNECTION_STRING=$(printf '%s' "$NEON_CONNECTION_STRING" | tr -cd '[:print:]' \
    | sed -e 's/\[20[01]~//g')
  uri_pattern="(postgres(ql)?://[^[:space:]\"']+)"
  kv_pattern="((Host|Server)=[^\"']+)"
  if [[ $NEON_CONNECTION_STRING =~ $uri_pattern ]]; then
    NEON_CONNECTION_STRING=${BASH_REMATCH[1]}
  elif [[ $NEON_CONNECTION_STRING =~ $kv_pattern ]]; then
    NEON_CONNECTION_STRING=$(printf '%s' "${BASH_REMATCH[1]}" | sed -e 's/[[:space:]]*$//')
  fi
  case "$NEON_CONNECTION_STRING" in
    postgres://*|postgresql://*)
      # Neon's default copy is a URI; Npgsql only takes key-value, so convert it here.
      # postgresql://user:password@host[:port]/database?params
      rest=${NEON_CONNECTION_STRING#*://}
      creds=${rest%%@*}; hostpart=${rest#*@}
      db_user=${creds%%:*}; db_password=${creds#*:}
      hostport=${hostpart%%/*}; db_name=${hostpart#*/}; db_name=${db_name%%\?*}
      db_host=${hostport%%:*}
      NEON_CONNECTION_STRING="Host=$db_host;Database=$db_name;Username=$db_user;Password=$db_password;SSL Mode=Require"
      case "$hostport" in *:*) NEON_CONNECTION_STRING="$NEON_CONNECTION_STRING;Port=${hostport##*:}" ;; esac
      unset rest creds hostpart db_user db_password hostport db_name db_host
      echo "Converted the postgresql:// URI to key-value form."
      ;;
  esac
  case "$NEON_CONNECTION_STRING" in
    Host=*|host=*|Server=*|server=*) ;;
    "") die "Nothing usable was received ($NEON_RAW_LENGTH characters typed). In Git Bash paste with Shift+Insert or right-click → Paste (Ctrl+V does not work), then press Enter." ;;
    *) # The first 10 characters are the scheme or key name, never the password.
       die "Not a connection string. Received $NEON_RAW_LENGTH characters starting with: '${NEON_CONNECTION_STRING:0:10}…'" ;;
  esac

  echo "Production API key for X-Api-Key (blank = generate one). Input is hidden:"
  read -rs API_KEY; echo
  GENERATED_KEY=false
  if [ -z "$API_KEY" ]; then
    API_KEY=$(openssl rand -hex 32)
    GENERATED_KEY=true
  fi

  step "Building the first image in ACR ($IMAGE_NAME:$GIT_SHA)"
  # Build from a clean export of HEAD, not the working folder: az acr build would otherwise
  # pack node_modules, .next and anything a running dev server holds locked (it does not
  # apply .dockerignore while packing), and the image would not match the SHA it is stamped with.
  BUILD_CONTEXT=$(mktemp -d)
  trap 'rm -rf "$BUILD_CONTEXT"' EXIT
  git archive HEAD | tar -x -C "$BUILD_CONTEXT"
  # cygpath: az is a Windows program and cannot open Git Bash's /tmp-style paths.
  if command -v cygpath >/dev/null; then BUILD_CONTEXT_ARG=$(cygpath -w "$BUILD_CONTEXT"); else BUILD_CONTEXT_ARG=$BUILD_CONTEXT; fi
  az acr build -r "$ACR_NAME" \
    -t "$IMAGE_NAME:$GIT_SHA" -t "$IMAGE_NAME:latest" \
    -f F1Predictor.WebApi/Dockerfile \
    --build-arg "GIT_SHA=$GIT_SHA" \
    "$BUILD_CONTEXT_ARG"

  step "Creating the container app"
  az containerapp create -g "$RG" -n "$CONTAINER_APP" \
    --environment "$ACA_ENV" \
    --image "$ACR_LOGIN_SERVER/$IMAGE_NAME:$GIT_SHA" \
    --registry-server "$ACR_LOGIN_SERVER" --registry-identity system \
    --ingress external --target-port 8080 \
    --cpu 0.5 --memory 1.0Gi \
    --min-replicas 0 --max-replicas 1 \
    --secrets "neon-connection-string=$NEON_CONNECTION_STRING" "api-key=$API_KEY" \
    --env-vars "ConnectionStrings__ProdDb=secretref:neon-connection-string" \
               "Security__ApiKey=secretref:api-key" \
               "Frontend__BaseUrl=$FRONTEND_URL" \
    -o none
  unset NEON_CONNECTION_STRING
  echo "Created."
  EXPECTED_BUILD="$GIT_SHA"
fi
APP_ID_RESOURCE=$(az containerapp show -g "$RG" -n "$CONTAINER_APP" --query id -o tsv)
APP_URL="https://$(az containerapp show -g "$RG" -n "$CONTAINER_APP" --query properties.configuration.ingress.fqdn -o tsv)"

# ---- GitHub OIDC -------------------------------------------------------------------------

step "GitHub Actions sign-in (OIDC app registration $OIDC_APP_NAME)"
CLIENT_ID=$(az ad app list --display-name "$OIDC_APP_NAME" --query "[0].appId" -o tsv)
if [ -n "$CLIENT_ID" ]; then
  echo "App registration exists: $CLIENT_ID"
else
  CLIENT_ID=$(az ad app create --display-name "$OIDC_APP_NAME" --query appId -o tsv)
  echo "Created app registration: $CLIENT_ID"
fi
SP_OBJECT_ID=$(az ad sp show --id "$CLIENT_ID" --query id -o tsv 2>/dev/null || true)
if [ -z "$SP_OBJECT_ID" ]; then
  SP_OBJECT_ID=$(az ad sp create --id "$CLIENT_ID" --query id -o tsv)
  echo "Created service principal."
fi

# GitHub's IMMUTABLE subject form, with numeric owner/repo IDs. The older
# repo:<owner>/<repo>:... form fails with AADSTS700213 — see .claude/rules/setup.md.
OWNER_ID=$(gh api "repos/$GH_REPO" --jq '.owner.id')
REPO_ID=$(gh api "repos/$GH_REPO" --jq '.id')
SUBJECT_PREFIX="repo:${GH_REPO%%/*}@$OWNER_ID/${GH_REPO##*/}@$REPO_ID"

add_federated_credential() {
  local name="$1" subject="$2"
  if [ -n "$(az ad app federated-credential list --id "$CLIENT_ID" --query "[?name=='$name'].name" -o tsv)" ]; then
    echo "Federated credential $name exists."
  else
    # Via a file, not inline: on Windows az is a .cmd wrapper, and JSON's double quotes do not
    # survive cmd.exe's argument handling. Relative path, because MSYS_NO_PATHCONV stops Git
    # Bash translating a /tmp path into one the Windows-side Python could open.
    local params="./.federated-credential.$$.json"
    cat >"$params" <<JSON
{
  "name": "$name",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "$subject",
  "audiences": ["api://AzureADTokenExchange"]
}
JSON
    az ad app federated-credential create --id "$CLIENT_ID" --parameters "@$params" -o none
    rm -f "$params"
    echo "Created federated credential $name -> $subject"
  fi
}
add_federated_credential "github-$GH_ENVIRONMENT" "$SUBJECT_PREFIX:environment:$GH_ENVIRONMENT"
add_federated_credential "github-main" "$SUBJECT_PREFIX:ref:refs/heads/main"

assign_role() {
  local role="$1" scope="$2"
  if [ "$(az role assignment list --assignee "$SP_OBJECT_ID" --role "$role" --scope "$scope" --query "length(@)" -o tsv)" != "0" ]; then
    echo "$role already assigned."
  else
    az role assignment create --assignee-object-id "$SP_OBJECT_ID" --assignee-principal-type ServicePrincipal \
      --role "$role" --scope "$scope" -o none
    echo "Assigned $role."
  fi
}
# Least privilege: push to this registry, manage this one app — nothing at resource-group scope.
assign_role AcrPush "$ACR_ID"
assign_role Contributor "$APP_ID_RESOURCE"

# ---- GitHub repo configuration -----------------------------------------------------------

step "GitHub repo configuration ($GH_REPO)"
gh api --method PUT "repos/$GH_REPO/environments/$GH_ENVIRONMENT" --silent
echo "Environment $GH_ENVIRONMENT ready."

gh variable set REGISTRY       --repo "$GH_REPO" --body "$ACR_LOGIN_SERVER"
gh variable set REGISTRY_NAME  --repo "$GH_REPO" --body "$ACR_NAME"
gh variable set IMAGE_NAME     --repo "$GH_REPO" --body "$IMAGE_NAME"
gh variable set RESOURCE_GROUP --repo "$GH_REPO" --body "$RG"
gh variable set CONTAINER_APP  --repo "$GH_REPO" --body "$CONTAINER_APP"
gh variable set APP_URL        --repo "$GH_REPO" --body "$APP_URL"
# Identifiers, not credentials — OIDC means there is no client secret anywhere.
gh secret set AZURE_CLIENT_ID       --repo "$GH_REPO" --body "$CLIENT_ID"
gh secret set AZURE_TENANT_ID       --repo "$GH_REPO" --body "$TENANT_ID"
gh secret set AZURE_SUBSCRIPTION_ID --repo "$GH_REPO" --body "$SUBSCRIPTION_ID"

# ---- Smoke test --------------------------------------------------------------------------

step "Waiting for $APP_URL/health (cold start can take a minute)"
APP_URL="$APP_URL" bash .github/scripts/wait-for-health.sh "$EXPECTED_BUILD"

# ---- Summary -----------------------------------------------------------------------------

step "Done"
cat <<EOF
API:            $APP_URL
API docs:       $APP_URL/scalar
Registry:       $ACR_LOGIN_SERVER
Container app:  $CONTAINER_APP (resource group $RG, $LOCATION)

Next:
  - On Vercel, set NEXT_PUBLIC_API_URL=$APP_URL and NEXT_PUBLIC_API_KEY to the API key, then redeploy.
  - Once the Vercel URL exists, allow it through CORS:
      az containerapp update -g $RG -n $CONTAINER_APP --set-env-vars Frontend__BaseUrl=https://<your-site>
  - Pushes to main now deploy automatically (.github/workflows/deploy.yml).
EOF

if [ "${GENERATED_KEY:-false}" = "true" ]; then
  echo
  echo "Generated API key (shown once — store it in your password manager and on Vercel):"
  echo "  $API_KEY"
fi
