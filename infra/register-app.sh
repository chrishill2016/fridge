#!/usr/bin/env bash
# Creates (or updates) the single-tenant Entra app registration used for sign-in.
# One registration serves both sides: the app signs in with it, and the API accepts its tokens.
# Usage: infra/register-app.sh [pages-url] [display-name]
# Requires: az login as someone who can create app registrations in the tenant.
set -euo pipefail

pages="${1:-https://chrishill2016.github.io/fridge}"
name="${2:-Fridge Chef}"
scope_name="access_as_user"
graph="https://graph.microsoft.com/v1.0"

app_id="$(az ad app list --display-name "$name" --query "[0].appId" -o tsv)"
if [ -z "$app_id" ]; then
  echo "Creating app registration '$name' (single tenant)"
  app_id="$(az ad app create --display-name "$name" --sign-in-audience AzureADMyOrg --query appId -o tsv)"
else
  echo "Updating existing app registration '$name'"
fi
object_id="$(az ad app show --id "$app_id" --query id -o tsv)"

# Keep the scope's ID stable across runs: changing it would invalidate existing consents.
scope_id="$(az ad app show --id "$app_id" --query "api.oauth2PermissionScopes[?value=='$scope_name'].id | [0]" -o tsv)"
[ -z "$scope_id" ] && scope_id="$(cat /proc/sys/kernel/random/uuid)"

redirects=()
for base in "$pages" "http://localhost:5199" "https://localhost:7290"; do
  redirects+=("\"$base/authentication/login-callback\"" "\"$base/authentication/logout-callback\"")
done
redirect_json="$(IFS=,; echo "${redirects[*]}")"

# 1. API identifier, the access_as_user scope, v2 tokens and SPA redirect URIs.
az rest --method PATCH --uri "$graph/applications/$object_id" --headers "Content-Type=application/json" --body "{
  \"identifierUris\": [\"api://$app_id\"],
  \"spa\": { \"redirectUris\": [$redirect_json] },
  \"api\": {
    \"requestedAccessTokenVersion\": 2,
    \"oauth2PermissionScopes\": [{
      \"id\": \"$scope_id\",
      \"value\": \"$scope_name\",
      \"type\": \"User\",
      \"isEnabled\": true,
      \"adminConsentDisplayName\": \"Use Fridge Chef\",
      \"adminConsentDescription\": \"Lets the Fridge Chef app call its API as the signed-in user.\",
      \"userConsentDisplayName\": \"Use Fridge Chef\",
      \"userConsentDescription\": \"Lets the Fridge Chef app call its API as you.\"
    }]
  }
}"

# 2. Pre-authorise the app for its own scope, so sign-in shows no consent prompt.
#    Separate call: Graph rejects pre-authorising a scope created in the same request.
az rest --method PATCH --uri "$graph/applications/$object_id" --headers "Content-Type=application/json" --body "{
  \"api\": { \"preAuthorizedApplications\": [{ \"appId\": \"$app_id\", \"delegatedPermissionIds\": [\"$scope_id\"] }] }
}"

# 3. The enterprise application (service principal) the tenant signs users in to.
if [ -z "$(az ad sp list --filter "appId eq '$app_id'" --query "[0].id" -o tsv)" ]; then
  az ad sp create --id "$app_id" >/dev/null
fi

tenant_id="$(az account show --query tenantId -o tsv)"
me="$(az ad signed-in-user show --query id -o tsv)"

cat <<OUT

Done.
  Client ID:        $app_id
  Tenant ID:        $tenant_id
  API scope:        api://$app_id/$scope_name
  Your object ID:   $me
  Redirect URIs:    $pages, http://localhost:5199, https://localhost:7290 (…/authentication/login-callback)

For the Function (local.settings.json now, app settings in phase 6):
  "Auth__TenantId": "$tenant_id",
  "Auth__ClientId": "$app_id",
  "Auth__AllowedObjectIds__0": "$me"

For the app: put the client ID in src/FridgeChef/wwwroot/appsettings.json (AzureAd:ClientId).
OUT
