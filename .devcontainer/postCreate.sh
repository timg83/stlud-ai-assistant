#!/usr/bin/env bash

set -euo pipefail

echo "Restoring backend dependencies"
dotnet restore src/backend/SchoolAssistant.Api/SchoolAssistant.Api.csproj

echo "Installing frontend dependencies"
npm --prefix src/frontend/chat-widget ci

echo "Devcontainer setup complete"