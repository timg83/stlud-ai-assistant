# School AI Assistent

Deze repository bevat de eerste implementatieskeleton voor een brongecontroleerde AI-assistent voor een schoolwebsite.

## Structuur

- `adr/` architectuurbeslissingen
- `docs/` ontwerpdocumentatie
- `src/backend/SchoolAssistant.Api/` ASP.NET Core API
- `src/frontend/chat-widget/` React widget
- `infra/terraform/` infrastructuur-skeleton
- `team/` rolprompts, werkafspraken en checklists

## Status

Dit is een eerste end-to-end skeleton. Azure-integraties zijn als interfaces en configuratiepunten opgenomen, maar nog niet gekoppeld aan echte resources.

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
