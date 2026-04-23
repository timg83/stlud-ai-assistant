# School AI Assistent

Deze repository bevat een brongecontroleerde AI-assistent voor een schoolwebsite, gebouwd op Azure AI Foundry.

## Structuur

- `adr/` architectuurbeslissingen
- `docs/` ontwerpdocumentatie
- `src/backend/SchoolAssistant.Api/` ASP.NET Core API (proxy, rate limiting, RAG-orchestratie)
- `src/frontend/chat-widget/` React widget
- `infra/terraform/` volledige Azure-infrastructuur (AI Foundry, Container Apps, AI Search, Cosmos DB, etc.)
- `team/` rolprompts, werkafspraken en checklists
- `scripts/` lokale dev- en deployment-scripts

## Status

Walking skeleton is operationeel. De backend draait op Azure Container Apps, de frontend als static website op Azure Storage. Azure AI Foundry (hub, project, AI Services en modeldeployments) wordt volledig via Terraform geprovisioned. Service-implementaties zijn nog placeholders — de interfaces en configuratiepunten staan klaar voor echte Azure SDK-integratie.

## Devcontainer

De repository bevat een devcontainer in `.devcontainer/` met .NET 10, Node.js 22, Terraform 1.7 en Azure CLI.

Open de repository in VS Code en kies `Dev Containers: Reopen in Container` om de ontwikkelomgeving op te starten. Tijdens de eerste build voert de container automatisch `dotnet restore` uit voor de backend en `npm ci` voor de widget.

## Walking skeleton starten

Start frontend en backend samen met:

```bash
./scripts/start-walking-skeleton.sh
```

De backend draait dan op `http://localhost:5000` en de widget op `http://localhost:5173`.

Het script controleert eerst de afhankelijkheden, start daarna beide processen en ruimt ze op wanneer je het script stopt met `Ctrl+C`.
