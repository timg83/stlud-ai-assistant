## Technische Architectuur (Azure-voorkeur)

### Voorgestelde Services

1. Frontend

- Bestaande schoolwebsite plus ingesloten chatwidget (JavaScript snippet).

2. Backend/API

- Azure App Service of Azure Container Apps voor Chat API en beheer-API.

3. LLM

- Azure OpenAI voor antwoordgeneratie en embeddings.

4. Search/Index

- Azure AI Search voor hybride retrieval.

5. Storage

- Azure Blob Storage voor bronbestanden en ingest artifacts.

6. Metadata en transactiegegevens

- Azure SQL of Cosmos DB voor contentstatus, gebruikersacties, workflow.

7. Secrets en identiteiten

- Azure Key Vault plus Managed Identities.

8. Monitoring

- Application Insights plus Log Analytics.

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
