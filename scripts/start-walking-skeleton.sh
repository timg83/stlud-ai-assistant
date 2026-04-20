#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BACKEND_DIR="$ROOT_DIR/src/backend/SchoolAssistant.Api"
FRONTEND_DIR="$ROOT_DIR/src/frontend/chat-widget"

BACKEND_URL="${BACKEND_URL:-http://0.0.0.0:5000}"
FRONTEND_HOST="${FRONTEND_HOST:-0.0.0.0}"
FRONTEND_PORT="${FRONTEND_PORT:-5173}"
PUBLIC_API_BASE_URL="${VITE_API_BASE_URL:-http://localhost:5000}"
BACKEND_PORT="${BACKEND_PORT:-5000}"

backend_pid=""
frontend_pid=""

cleanup() {
  local exit_code=$?

  if [[ -n "$frontend_pid" ]] && kill -0 "$frontend_pid" 2>/dev/null; then
    kill "$frontend_pid" 2>/dev/null || true
    wait "$frontend_pid" 2>/dev/null || true
  fi

  if [[ -n "$backend_pid" ]] && kill -0 "$backend_pid" 2>/dev/null; then
    kill "$backend_pid" 2>/dev/null || true
    wait "$backend_pid" 2>/dev/null || true
  fi

  exit "$exit_code"
}

trap cleanup EXIT INT TERM

require_command() {
  local command_name="$1"

  if ! command -v "$command_name" >/dev/null 2>&1; then
    echo "[walking-skeleton] Vereist commando ontbreekt: $command_name" >&2
    exit 1
  fi
}

assert_port_available() {
  local port="$1"
  local label="$2"

  if lsof -tiTCP:"$port" -sTCP:LISTEN >/dev/null 2>&1; then
    echo "[walking-skeleton] Poort $port voor $label is al in gebruik. Stop het bestaande proces of kies een andere poort." >&2
    exit 1
  fi
}

require_command dotnet
require_command npm
require_command lsof

assert_port_available "$BACKEND_PORT" "backend"
assert_port_available "$FRONTEND_PORT" "frontend"

echo "[walking-skeleton] Controleer backend dependencies"
dotnet restore "$BACKEND_DIR/SchoolAssistant.Api.csproj" >/dev/null

echo "[walking-skeleton] Controleer frontend dependencies"
if [[ ! -d "$FRONTEND_DIR/node_modules" ]]; then
  npm ci --prefix "$FRONTEND_DIR"
fi

echo "[walking-skeleton] Start backend op $BACKEND_URL"
ASPNETCORE_URLS="$BACKEND_URL" \
  dotnet run --project "$BACKEND_DIR/SchoolAssistant.Api.csproj" &
backend_pid=$!

echo "[walking-skeleton] Start frontend op http://localhost:$FRONTEND_PORT"
VITE_API_BASE_URL="$PUBLIC_API_BASE_URL" \
  npm run dev --prefix "$FRONTEND_DIR" -- --host "$FRONTEND_HOST" --port "$FRONTEND_PORT" --strictPort &
frontend_pid=$!

echo "[walking-skeleton] Applicatie gestart"
echo "[walking-skeleton] Backend:  http://localhost:5000/health"
echo "[walking-skeleton] Frontend: http://localhost:$FRONTEND_PORT"
echo "[walking-skeleton] Stop met Ctrl+C"

wait "$backend_pid" "$frontend_pid"