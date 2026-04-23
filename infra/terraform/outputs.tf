output "resource_group_name" {
  description = "Name of the Azure resource group."
  value       = azurerm_resource_group.main.name
}

output "backend_app_name" {
  description = "Name of the backend Azure Container App."
  value       = azurerm_container_app.backend.name
}

output "backend_url" {
  description = "Public HTTPS URL of the backend container app."
  value       = "https://${azurerm_container_app.backend.ingress[0].fqdn}"
}

output "frontend_url" {
  description = "Static website URL for the frontend widget."
  value       = azurerm_storage_account.main.primary_web_endpoint
}

output "storage_account_name" {
  description = "Name of the storage account used for source documents."
  value       = azurerm_storage_account.main.name
}

output "blob_container_name" {
  description = "Private blob container for source documents."
  value       = azurerm_storage_container.source_documents.name
}

output "container_registry_name" {
  description = "Azure Container Registry name for backend images."
  value       = azurerm_container_registry.main.name
}

output "container_registry_login_server" {
  description = "Azure Container Registry login server."
  value       = azurerm_container_registry.main.login_server
}

output "cosmosdb_account_name" {
  description = "Name of the Cosmos DB account."
  value       = azurerm_cosmosdb_account.main.name
}

output "cosmosdb_endpoint" {
  description = "Cosmos DB endpoint consumed by the backend."
  value       = azurerm_cosmosdb_account.main.endpoint
}

output "search_service_name" {
  description = "Name of the Azure AI Search service."
  value       = azurerm_search_service.main.name
}

output "search_endpoint" {
  description = "Azure AI Search endpoint."
  value       = "https://${azurerm_search_service.main.name}.search.windows.net"
}

output "key_vault_name" {
  description = "Name of the Azure Key Vault."
  value       = azurerm_key_vault.main.name
}

output "key_vault_uri" {
  description = "URI of the Azure Key Vault."
  value       = azurerm_key_vault.main.vault_uri
}

output "application_insights_connection_string" {
  description = "Application Insights connection string for the backend."
  value       = azurerm_application_insights.main.connection_string
  sensitive   = true
}

# ---------------------------------------------------------------------------
# Azure AI Foundry
# ---------------------------------------------------------------------------

output "ai_services_name" {
  description = "Azure AI Services account name."
  value       = azurerm_ai_services.main.name
}

output "ai_services_endpoint" {
  description = "Azure AI Services endpoint (used for OpenAI API calls)."
  value       = azurerm_ai_services.main.endpoint
}

output "ai_foundry_name" {
  description = "Azure AI Foundry hub name."
  value       = azurerm_ai_foundry.main.name
}

output "ai_foundry_project_name" {
  description = "Azure AI Foundry project name."
  value       = azurerm_ai_foundry_project.main.name
}

output "chat_deployment_name" {
  description = "Chat model deployment name."
  value       = azurerm_cognitive_deployment.chat.name
}

output "embedding_deployment_name" {
  description = "Embedding model deployment name."
  value       = azurerm_cognitive_deployment.embedding.name
}
