# Terraform

Deze map bevat de volledige Azure-infrastructuur voor de MVP, inclusief Azure AI Foundry voor het AI-platform.

## Wat wordt aangemaakt

- Resource group
- Log Analytics workspace en Application Insights
- **Azure AI Services** account met OpenAI-modeldeployments (chat + embedding)
- **Azure AI Foundry** hub (verbindt Storage, Key Vault en Application Insights)
- **Azure AI Foundry Project** als teamworkspace
- Azure Container Registry voor backend images
- Azure Container Apps environment en Container App voor de backend
- Storage account en private blob container voor brondocumenten
- Static website hosting op hetzelfde storage account voor de frontend widget
- Cosmos DB account en SQL database voor metadata en workflowstatus
- Azure AI Search service
- Key Vault met RBAC ingeschakeld
- Managed identity op de backend-container met RBAC-rechten op Key Vault, Blob Storage, ACR pull, Cognitive Services OpenAI, AI Search en Cosmos DB

## Gebruik

Initialiseer en valideer lokaal:

```bash
terraform -chdir=infra/terraform init
terraform -chdir=infra/terraform validate
```

Maak daarna een lokale variantenfile op basis van het voorbeeld:

```bash
cp infra/terraform/terraform.tfvars.example infra/terraform/terraform.tfvars
```

Deployen naar Azure-infra:

```bash
az login
terraform -chdir=infra/terraform plan
terraform -chdir=infra/terraform apply
```

Deployen van backend-image en frontend-build:

```bash
./scripts/deploy-azure-runtime.sh
```

## Belangrijkste variabelen

- `name_prefix`: korte naam voor alle resources, bijvoorbeeld `schoolai`
- `environment`: bijvoorbeeld `dev`, `test` of `prod`
- `location`: Azure-regio, standaard `westeurope`
- `chat_model_name` / `chat_model_version`: OpenAI-chatmodel en versie
- `embedding_model_name` / `embedding_model_version`: OpenAI-embeddingmodel en versie
- `chat_model_capacity` / `embedding_model_capacity`: TPM-capaciteit (in duizendtallen)
- `allowed_origins`: extra widget-origins voor CORS naast de automatisch gegenereerde static website URL
- `backend_image_tag`: image tag voor de backend container

## Opmerkingen

- De backend Container App krijgt omgevingsvariabelen mee die direct aansluiten op [src/backend/SchoolAssistant.Api/appsettings.json](/workspaces/LudgardisAI/src/backend/SchoolAssistant.Api/appsettings.json).
- Voor Cosmos DB en Azure AI Search is nog geen volledige data-plane RBAC voor ingest- en beheerworkflows toegevoegd. Dat is de volgende stap zodra de echte integraties in de backend landen.
