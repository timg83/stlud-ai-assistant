#!/usr/bin/env bash
# -------------------------------------------------------------------
# upload-and-ingest-pdf.sh
#
# Uploadt een PDF naar Azure Blob Storage en triggert de ingestion
# pipeline via de backend API.
#
# Gebruik:
#   ./scripts/upload-and-ingest-pdf.sh <pad-naar-pdf> [titel] [taal]
#
# Voorbeeld:
#   ./scripts/upload-and-ingest-pdf.sh docs/schoolbeleid.pdf "Schoolbeleid 2025-2026" nl
# -------------------------------------------------------------------

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TF_DIR="$ROOT_DIR/infra/terraform"

PDF_PATH="${1:?Gebruik: $0 <pad-naar-pdf> [titel] [taal]}"
TITLE="${2:-$(basename "$PDF_PATH" .pdf)}"
LANGUAGE="${3:-nl}"

if [[ ! -f "$PDF_PATH" ]]; then
  echo "[upload-and-ingest] Bestand niet gevonden: $PDF_PATH" >&2
  exit 1
fi

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "[upload-and-ingest] Vereist commando ontbreekt: $1" >&2
    exit 1
  fi
}

require_command az
require_command terraform
require_command jq
require_command curl

echo "[upload-and-ingest] Valideer Azure login"
az account show >/dev/null

BLOB_NAME="$(basename "$PDF_PATH")"
STORAGE_ACCOUNT="$(terraform -chdir="$TF_DIR" output -raw storage_account_name)"
CONTAINER_NAME="$(terraform -chdir="$TF_DIR" output -raw blob_container_name)"
BACKEND_URL="$(terraform -chdir="$TF_DIR" output -raw backend_url)"

echo "[upload-and-ingest] Upload $PDF_PATH naar $STORAGE_ACCOUNT/$CONTAINER_NAME/$BLOB_NAME"
az storage blob upload \
  --account-name "$STORAGE_ACCOUNT" \
  --container-name "$CONTAINER_NAME" \
  --name "$BLOB_NAME" \
  --file "$PDF_PATH" \
  --auth-mode login \
  --overwrite

echo "[upload-and-ingest] Trigger ingestion via backend API"
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST \
  "${BACKEND_URL}/api/content/ingest" \
  -H "Content-Type: application/json" \
  -d "$(jq -n --arg blob "$BLOB_NAME" --arg title "$TITLE" --arg lang "$LANGUAGE" \
    '{blobName: $blob, title: $title, language: $lang}')")

HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [[ "$HTTP_CODE" -ge 200 && "$HTTP_CODE" -lt 300 ]]; then
  echo "[upload-and-ingest] Ingestion geslaagd!"
  echo "$BODY" | jq .
else
  echo "[upload-and-ingest] Ingestion mislukt (HTTP $HTTP_CODE):" >&2
  echo "$BODY" >&2
  exit 1
fi
