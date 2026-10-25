#!/usr/bin/env bash
# Lets GitHub Actions deploy the Function's code to Azure with OIDC: no stored secrets.
# Creates an app registration whose only credential is a federated trust for this repo's main branch,
# and gives it Website Contributor on the Function App only (it can deploy code, not change infrastructure).
# Usage: infra/setup-github-deploy.sh [owner/repo] [resource-group]
# Run after infra/deploy.sh has created the Function App.
set -euo pipefail

repo="${1:-chrishill2016/fridge}"
rg="${2:-rg-fridge-chef}"
name="fridge-github-deploy"

# GitHub's OIDC subject comes in two forms. Newer repos send the immutable one, with numeric owner and
# repo IDs (repo:owner@123/name@456:ref:...); older ones send names only. Trust both, so renames
# or GitHub-side changes don't break deploys.
owner="${repo%%/*}"
repo_name="${repo#*/}"
ids="$(curl -fsS "https://api.github.com/repos/$repo" 2>/dev/null \
  | python3 -c "import json,sys; d=json.load(sys.stdin); print(d['owner']['id'], d['id'])" 2>/dev/null || true)"
if [ -z "$ids" ] && command -v gh >/dev/null; then
  ids="$(gh api "repos/$repo" --jq '"\(.owner.id) \(.id)"' 2>/dev/null || true)"
fi
subjects=("repo:${repo}:ref:refs/heads/main")
if [ -n "$ids" ]; then
  read -r owner_id repo_id <<<"$ids"
  subjects+=("repo:${owner}@${owner_id}/${repo_name}@${repo_id}:ref:refs/heads/main")
else
  echo "Couldn't look up $repo's IDs (private repo? run 'gh auth login' and re-run). Trusting the name-only subject." >&2
fi

function_app="$(az functionapp list -g "$rg" --query "[0].name" -o tsv)"
if [ -z "$function_app" ]; then
  echo "No Function App in $rg yet. Run infra/deploy.sh first." >&2
  exit 1
fi
function_id="$(az functionapp show -g "$rg" -n "$function_app" --query id -o tsv)"

app_id="$(az ad app list --display-name "$name" --query "[0].appId" -o tsv)"
if [ -z "$app_id" ]; then
  echo "Creating app registration '$name'"
  app_id="$(az ad app create --display-name "$name" --sign-in-audience AzureADMyOrg --query appId -o tsv)"
fi

sp_id="$(az ad sp list --filter "appId eq '$app_id'" --query "[0].id" -o tsv)"
if [ -z "$sp_id" ]; then
  sp_id="$(az ad sp create --id "$app_id" --query id -o tsv)"
fi

for i in "${!subjects[@]}"; do
  subject="${subjects[$i]}"
  if [ -z "$(az ad app federated-credential list --id "$app_id" --query "[?subject=='$subject'].name" -o tsv)" ]; then
    echo "Trusting GitHub Actions: $subject"
    az ad app federated-credential create --id "$app_id" --parameters "{
      \"name\": \"github-main-$i\",
      \"issuer\": \"https://token.actions.githubusercontent.com\",
      \"subject\": \"$subject\",
      \"audiences\": [\"api://AzureADTokenExchange\"]
    }" >/dev/null
  fi
done

if [ -z "$(az role assignment list --assignee "$sp_id" --scope "$function_id" --role "Website Contributor" --query "[0].id" -o tsv)" ]; then
  echo "Granting Website Contributor on $function_app"
  az role assignment create --assignee-object-id "$sp_id" --assignee-principal-type ServicePrincipal \
    --role "Website Contributor" --scope "$function_id" >/dev/null
fi

tenant_id="$(az account show --query tenantId -o tsv)"
subscription_id="$(az account show --query id -o tsv)"

cat <<OUT

Done. Add these as GitHub repository *variables* (Settings → Secrets and variables → Actions → Variables).
They're IDs, not secrets, so they don't go under Secrets:

  AZURE_CLIENT_ID          $app_id
  AZURE_TENANT_ID          $tenant_id
  AZURE_SUBSCRIPTION_ID    $subscription_id
  AZURE_FUNCTIONAPP_NAME   $function_app

Or, if the GitHub CLI is signed in here (gh auth login):
  gh variable set AZURE_CLIENT_ID --repo $repo --body $app_id
  gh variable set AZURE_TENANT_ID --repo $repo --body $tenant_id
  gh variable set AZURE_SUBSCRIPTION_ID --repo $repo --body $subscription_id
  gh variable set AZURE_FUNCTIONAPP_NAME --repo $repo --body $function_app
OUT
