#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TF_DIR="$ROOT_DIR/infra/terraform"
BACKEND_DIR="$ROOT_DIR/src/backend/SchoolAssistant.Api"
FRONTEND_DIR="$ROOT_DIR/src/frontend/chat-widget"

require_command() {
  local command_name="$1"

  if ! command -v "$command_name" >/dev/null 2>&1; then
    echo "[deploy-azure-runtime] Vereist commando ontbreekt: $command_name" >&2
    exit 1
  fi
}

require_command az
require_command terraform
require_command npm
require_command jq

echo "[deploy-azure-runtime] Valideer Azure login"
az account show >/dev/null

echo "[deploy-azure-runtime] Bootstrap infrastructuur voor registry en runtime"
terraform -chdir="$TF_DIR" apply -auto-approve \
  -target=random_string.resource_suffix \
  -target=azurerm_resource_group.main \
  -target=azurerm_log_analytics_workspace.main \
  -target=azurerm_application_insights.main \
  -target=azurerm_storage_account.main \
  -target=azurerm_storage_container.source_documents \
  -target=azurerm_cosmosdb_account.main \
  -target=azurerm_cosmosdb_sql_database.main \
  -target=azurerm_search_service.main \
  -target=azurerm_key_vault.main \
  -target=azurerm_ai_services.main \
  -target=azurerm_ai_foundry.main \
  -target=azurerm_ai_foundry_project.main \
  -target=azurerm_cognitive_deployment.chat \
  -target=azurerm_cognitive_deployment.embedding \
  -target=azurerm_container_registry.main \
  -target=azurerm_container_app_environment.main

acr_name="$(terraform -chdir="$TF_DIR" output -raw container_registry_name)"
backend_image_tag="$(terraform -chdir="$TF_DIR" console <<< 'var.backend_image_tag' | tr -d '"')"
backend_image_name="$(terraform -chdir="$TF_DIR" console <<< 'var.backend_image_name' | tr -d '"')"

echo "[deploy-azure-runtime] Build backend image in Azure Container Registry"
az acr build \
  --registry "$acr_name" \
  --image "$backend_image_name:$backend_image_tag" \
  --file "$BACKEND_DIR/Dockerfile" \
  "$BACKEND_DIR"

echo "[deploy-azure-runtime] Pas volledige infrastructuur toe"
terraform -chdir="$TF_DIR" apply -auto-approve

backend_url="$(terraform -chdir="$TF_DIR" output -raw backend_url)"
storage_account_name="$(terraform -chdir="$TF_DIR" output -raw storage_account_name)"
storage_account_key="$(az storage account keys list --account-name "$storage_account_name" --query '[0].value' -o tsv)"

echo "[deploy-azure-runtime] Build frontend widget met backend URL $backend_url"
npm --prefix "$FRONTEND_DIR" ci
VITE_API_BASE_URL="$backend_url" npm --prefix "$FRONTEND_DIR" run build

echo "[deploy-azure-runtime] Upload frontend naar static website"
az storage blob upload-batch \
  --account-name "$storage_account_name" \
  --account-key "$storage_account_key" \
  --destination '$web' \
  --source "$FRONTEND_DIR/dist" \
  --overwrite

echo "[deploy-azure-runtime] Deployment voltooid"
echo "[deploy-azure-runtime] Frontend: $(terraform -chdir="$TF_DIR" output -raw frontend_url)"
echo "[deploy-azure-runtime] Backend:  $backend_url"