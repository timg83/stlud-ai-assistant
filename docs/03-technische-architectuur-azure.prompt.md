## Technische Architectuur (Azure)

### Services

1. Frontend

- Bestaande schoolwebsite plus ingesloten chatwidget (JavaScript snippet).
- Widget wordt gehost als static website op Azure Storage.

2. Backend/API

- Azure Container Apps voor Chat API en beheer-API.

3. AI Platform

- Azure AI Foundry (hub + project) met Azure AI Services.
- Modeldeployments (chat en embedding) via Terraform geprovisioned.

4. Search/Index

- Azure AI Search voor hybride retrieval (keyword + vector).

5. Storage

- Azure Blob Storage voor bronbestanden en ingest artifacts.

6. Metadata en transactiegegevens

- Azure Cosmos DB (SQL API) voor contentstatus, chattraces, workflow.

7. Secrets en identiteiten

- Azure Key Vault plus Managed Identities voor service-to-service authenticatie.

8. Monitoring

- Application Insights plus Log Analytics, geïntegreerd via AI Foundry hub.

### Deployments en Omgevingen

1. Omgevingen

- Dev, Test, Prod met gescheiden resources en sleutels.

2. CI/CD

- Pipeline met geautomatiseerde tests, security scans en gecontroleerde promotie.

3. Configuratie

- Feature flags voor talen, fallbackgedrag en retrievalinstellingen.

4. Infrastructure-as-code

- Alle services die gebruikt worden zijn te deployen middels IaC templates gemaakt in Terraform.

### Integraties

1. Website crawling/seed

- Gecontroleerde lijst van publieke URL's.

2. Handmatige upload

- PDF/Word via beheerportaal.

3. Toekomstige koppeling

- SharePoint/M365 connector na MVP.
