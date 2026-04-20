terraform {
  required_version = ">= 1.7.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }
}

provider "azurerm" {
  features {}
}

data "azurerm_client_config" "current" {}

resource "random_string" "resource_suffix" {
  length  = 5
  upper   = false
  special = false
  numeric = true
}

locals {
  application_name = "${var.name_prefix}-${var.environment}"
  tags = merge(var.tags, {
    application = var.name_prefix
    environment = var.environment
    managed-by  = "terraform"
  })

  storage_account_name    = substr("st${var.name_prefix}${var.environment}${random_string.resource_suffix.result}", 0, 24)
  container_app_name      = "ca-${local.application_name}"
  container_env_name      = "cae-${local.application_name}"
  container_registry_name = substr("cr${var.name_prefix}${var.environment}${random_string.resource_suffix.result}", 0, 50)
  cosmos_account_name     = "cosmos-${local.application_name}"
  search_service_name     = "srch-${local.application_name}"
  key_vault_name          = substr("kv-${local.application_name}-${random_string.resource_suffix.result}", 0, 24)
  log_analytics_name      = "log-${local.application_name}"
  app_insights_name       = "appi-${local.application_name}"
  ai_services_name        = "ais-${local.application_name}-${random_string.resource_suffix.result}"
  ai_foundry_name         = "aihub-${local.application_name}"
  ai_project_name         = "aiproj-${local.application_name}"

  frontend_origin           = trimsuffix(azurerm_storage_account.main.primary_web_endpoint, "/")
  effective_allowed_origins = distinct(concat(var.allowed_origins, [local.frontend_origin]))

  allowed_origin_app_settings = {
    for index, origin in local.effective_allowed_origins : "WidgetSecurity__AllowedOrigins__${index}" => origin
  }

  allowed_locale_app_settings = {
    for index, locale in var.allowed_locales : "WidgetSecurity__AllowedLocales__${index}" => locale
  }
}

resource "azurerm_resource_group" "main" {
  name     = "rg-${local.application_name}"
  location = var.location
  tags     = local.tags
}

resource "azurerm_log_analytics_workspace" "main" {
  name                = local.log_analytics_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = var.log_analytics_sku
  retention_in_days   = var.log_retention_in_days
  tags                = local.tags
}

resource "azurerm_application_insights" "main" {
  name                = local.app_insights_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"
  tags                = local.tags
}

resource "azurerm_storage_account" "main" {
  name                            = local.storage_account_name
  resource_group_name             = azurerm_resource_group.main.name
  location                        = azurerm_resource_group.main.location
  account_tier                    = "Standard"
  account_replication_type        = var.storage_replication_type
  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false
  public_network_access_enabled   = true
  shared_access_key_enabled       = true
  tags                            = local.tags
}

resource "azurerm_storage_account_static_website" "frontend" {
  storage_account_id = azurerm_storage_account.main.id
  index_document     = "index.html"
  error_404_document = "index.html"
}

resource "azurerm_storage_container" "source_documents" {
  name                  = var.blob_container_name
  storage_account_id    = azurerm_storage_account.main.id
  container_access_type = "private"
}

resource "azurerm_cosmosdb_account" "main" {
  name                          = local.cosmos_account_name
  location                      = azurerm_resource_group.main.location
  resource_group_name           = azurerm_resource_group.main.name
  offer_type                    = "Standard"
  kind                          = "GlobalDocumentDB"
  public_network_access_enabled = true
  free_tier_enabled             = var.cosmosdb_free_tier_enabled
  automatic_failover_enabled    = false
  tags                          = local.tags

  consistency_policy {
    consistency_level = "Session"
  }

  geo_location {
    location          = azurerm_resource_group.main.location
    failover_priority = 0
  }
}

resource "azurerm_cosmosdb_sql_database" "main" {
  name                = var.cosmosdb_database_name
  resource_group_name = azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.main.name
}

resource "azurerm_search_service" "main" {
  name                          = local.search_service_name
  resource_group_name           = azurerm_resource_group.main.name
  location                      = azurerm_resource_group.main.location
  sku                           = var.search_sku
  replica_count                 = var.search_replica_count
  partition_count               = var.search_partition_count
  local_authentication_enabled  = true
  public_network_access_enabled = true
  tags                          = local.tags
}

resource "azurerm_key_vault" "main" {
  name                          = local.key_vault_name
  location                      = azurerm_resource_group.main.location
  resource_group_name           = azurerm_resource_group.main.name
  tenant_id                     = data.azurerm_client_config.current.tenant_id
  sku_name                      = "standard"
  purge_protection_enabled      = true
  soft_delete_retention_days    = 90
  rbac_authorization_enabled    = true
  public_network_access_enabled = true
  tags                          = local.tags
}

# ---------------------------------------------------------------------------
# Azure AI Foundry – AI Services account, hub, project & model deployments
# ---------------------------------------------------------------------------

resource "azurerm_ai_services" "main" {
  name                         = local.ai_services_name
  location                     = azurerm_resource_group.main.location
  resource_group_name          = azurerm_resource_group.main.name
  sku_name                     = var.ai_services_sku
  custom_subdomain_name        = local.ai_services_name
  local_authentication_enabled = true
  public_network_access        = "Enabled"
  tags                         = local.tags
}

resource "azurerm_ai_foundry" "main" {
  name                    = local.ai_foundry_name
  location                = azurerm_resource_group.main.location
  resource_group_name     = azurerm_resource_group.main.name
  storage_account_id      = azurerm_storage_account.main.id
  key_vault_id            = azurerm_key_vault.main.id
  application_insights_id = azurerm_application_insights.main.id

  identity {
    type = "SystemAssigned"
  }

  tags = local.tags
}

resource "azurerm_ai_foundry_project" "main" {
  name               = local.ai_project_name
  location           = azurerm_resource_group.main.location
  ai_services_hub_id = azurerm_ai_foundry.main.id

  identity {
    type = "SystemAssigned"
  }

  tags = local.tags
}

resource "azurerm_cognitive_deployment" "chat" {
  name                 = var.chat_model_deployment_name
  cognitive_account_id = azurerm_ai_services.main.id

  model {
    format  = "OpenAI"
    name    = var.chat_model_name
    version = var.chat_model_version
  }

  sku {
    name     = "Standard"
    capacity = var.chat_model_capacity
  }
}

resource "azurerm_cognitive_deployment" "embedding" {
  name                 = var.embedding_model_deployment_name
  cognitive_account_id = azurerm_ai_services.main.id

  model {
    format  = "OpenAI"
    name    = var.embedding_model_name
    version = var.embedding_model_version
  }

  sku {
    name     = "Standard"
    capacity = var.embedding_model_capacity
  }
}

resource "azurerm_container_registry" "main" {
  name                = local.container_registry_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = var.container_registry_sku
  admin_enabled       = true
  tags                = local.tags
}

resource "azurerm_container_app_environment" "main" {
  name                       = local.container_env_name
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id
  tags                       = local.tags
}

resource "azurerm_container_app" "backend" {
  name                         = local.container_app_name
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"
  tags                         = local.tags

  identity {
    type = "SystemAssigned"
  }

  registry {
    server               = azurerm_container_registry.main.login_server
    username             = azurerm_container_registry.main.admin_username
    password_secret_name = "acr-password"
  }

  secret {
    name  = "acr-password"
    value = azurerm_container_registry.main.admin_password
  }

  ingress {
    allow_insecure_connections = false
    external_enabled           = true
    target_port                = var.backend_container_port
    transport                  = "auto"

    cors {
      allowed_origins = local.effective_allowed_origins
      allowed_methods = ["GET", "POST", "OPTIONS"]
      allowed_headers = ["*"]
    }

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  template {
    min_replicas = var.container_app_min_replicas
    max_replicas = var.container_app_max_replicas

    container {
      name   = "backend"
      image  = "${azurerm_container_registry.main.login_server}/${var.backend_image_name}:${var.backend_image_tag}"
      cpu    = var.backend_container_cpu
      memory = var.backend_container_memory

      env {
        name  = "ASPNETCORE_URLS"
        value = "http://0.0.0.0:${var.backend_container_port}"
      }

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.aspnetcore_environment
      }

      env {
        name  = "APPLICATIONINSIGHTS_CONNECTION_STRING"
        value = azurerm_application_insights.main.connection_string
      }

      env {
        name  = "ApplicationInsights__ConnectionString"
        value = azurerm_application_insights.main.connection_string
      }

      env {
        name  = "AzureOpenAi__Endpoint"
        value = azurerm_ai_services.main.endpoint
      }

      env {
        name  = "AzureOpenAi__ChatDeployment"
        value = azurerm_cognitive_deployment.chat.name
      }

      env {
        name  = "AzureOpenAi__EmbeddingDeployment"
        value = azurerm_cognitive_deployment.embedding.name
      }

      env {
        name  = "AzureAiSearch__Endpoint"
        value = "https://${azurerm_search_service.main.name}.search.windows.net"
      }

      env {
        name  = "AzureAiSearch__IndexName"
        value = var.search_index_name
      }

      env {
        name  = "CosmosDb__AccountEndpoint"
        value = azurerm_cosmosdb_account.main.endpoint
      }

      env {
        name  = "CosmosDb__DatabaseName"
        value = azurerm_cosmosdb_sql_database.main.name
      }

      env {
        name  = "BlobStorage__ContainerName"
        value = azurerm_storage_container.source_documents.name
      }

      env {
        name  = "RateLimiting__PermitLimit"
        value = tostring(var.rate_limit_permit_limit)
      }

      env {
        name  = "RateLimiting__WindowSeconds"
        value = tostring(var.rate_limit_window_seconds)
      }

      env {
        name  = "RateLimiting__QueueLimit"
        value = tostring(var.rate_limit_queue_limit)
      }

      env {
        name  = "WidgetSecurity__MaxQuestionLength"
        value = tostring(var.max_question_length)
      }

      dynamic "env" {
        for_each = local.allowed_origin_app_settings
        content {
          name  = env.key
          value = env.value
        }
      }

      dynamic "env" {
        for_each = local.allowed_locale_app_settings
        content {
          name  = env.key
          value = env.value
        }
      }
    }
  }
}

resource "azurerm_role_assignment" "app_key_vault_secrets_user" {
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_container_app.backend.identity[0].principal_id
}

resource "azurerm_role_assignment" "app_storage_blob_contributor" {
  scope                = azurerm_storage_account.main.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_container_app.backend.identity[0].principal_id
}

resource "azurerm_role_assignment" "app_acr_pull" {
  scope                = azurerm_container_registry.main.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_container_app.backend.identity[0].principal_id
}

resource "azurerm_role_assignment" "app_cognitive_services_user" {
  scope                = azurerm_ai_services.main.id
  role_definition_name = "Cognitive Services OpenAI User"
  principal_id         = azurerm_container_app.backend.identity[0].principal_id
}

resource "azurerm_role_assignment" "app_search_index_reader" {
  scope                = azurerm_search_service.main.id
  role_definition_name = "Search Index Data Reader"
  principal_id         = azurerm_container_app.backend.identity[0].principal_id
}

resource "azurerm_role_assignment" "app_cosmosdb_contributor" {
  scope                = azurerm_cosmosdb_account.main.id
  role_definition_name = "Cosmos DB Built-in Data Contributor"
  principal_id         = azurerm_container_app.backend.identity[0].principal_id
}
