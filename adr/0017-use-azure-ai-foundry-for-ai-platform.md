# ADR 0017: Use Azure AI Foundry for AI Platform

## Status

Accepted – supersedes [ADR 0006](0006-use-azure-openai-for-generation-and-embeddings.md)

## Context

ADR 0006 stelde vast dat Azure OpenAI als losse resource wordt gebruikt voor generatie en embeddings. In de praktijk vereist dit handmatig aanmaken van een Azure OpenAI-resource en het extern doorgeven van endpoints en deployment-namen aan de infrastructuur.

Microsoft biedt inmiddels **Azure AI Foundry** als geïntegreerd AI-platform dat Azure AI Services, Azure OpenAI, Azure AI Search en monitoring bundelt onder één hub-/projectmodel. Hiermee wordt het provisionen van het volledige AI-landschap via Terraform mogelijk, inclusief modeldeployments.

## Decision

We vervangen de losse Azure OpenAI-resource door **Azure AI Foundry**:

1. **`azurerm_ai_services`** – AI Services-account met OpenAI-modeldeployments (chat + embedding).
2. **`azurerm_ai_foundry`** – Hub die Storage Account, Key Vault en Application Insights verbindt.
3. **`azurerm_ai_foundry_project`** – Projectworkspace voor het team.
4. **`azurerm_cognitive_deployment`** – Declaratief provisionen van chat- en embeddingmodellen.

De backend container app ontvangt het AI Services-endpoint en deployment-namen automatisch via Terraform-outputs als environment variables. Managed Identity (RBAC-role `Cognitive Services OpenAI User`) wordt gebruikt voor authenticatie.

## Consequences

- Volledige AI-stack is nu Infrastructure-as-Code; geen handmatige Azure OpenAI-setup meer nodig.
- Modelversies en capaciteit (TPM) zijn via `terraform.tfvars` configureerbaar.
- AI Foundry-hub biedt één plek voor monitoring, prompt-experimenten en modelbeheer.
- Extra Azure-resources (AI Services, hub, project) verhogen licht de infrastructuurkosten (AI Services S0-tier).
- ADR 0006 is hiermee vervangen.
