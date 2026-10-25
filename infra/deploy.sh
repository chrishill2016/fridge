#!/usr/bin/env bash
# Deploys infra/main.bicep into a resource group, creating the group if needed.
# Usage: infra/deploy.sh <budget-email> [resource-group] [location]
# Requires: az login, and Contributor + User Access Administrator (or Owner) on the subscription or group.
set -euo pipefail

email="${1:?Usage: infra/deploy.sh <budget-email> [resource-group] [location]}"
rg="${2:-rg-fridge-chef}"
location="${3:-uksouth}"
here="$(cd "$(dirname "$0")" && pwd)"

if ! az group show --name "$rg" >/dev/null 2>&1; then
  echo "Creating resource group $rg in $location"
  az group create --name "$rg" --location "$location" --tags app=fridge-chef >/dev/null
fi

DEVELOPER_PRINCIPAL_ID="$(az ad signed-in-user show --query id -o tsv)"
BUDGET_EMAIL="$email"
export DEVELOPER_PRINCIPAL_ID BUDGET_EMAIL

echo "Deploying to $rg (developer object ID $DEVELOPER_PRINCIPAL_ID)"
az deployment group create \
  --resource-group "$rg" \
  --name "fridge-chef-$(date +%Y%m%d%H%M%S)" \
  --parameters "$here/main.bicepparam" \
  --query properties.outputs \
  --output json
