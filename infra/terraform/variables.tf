variable "name_prefix" {
  type        = string
  description = "Short lowercase prefix used in Azure resource names."
  default     = "schoolai"

  validation {
    condition     = can(regex("^[a-z0-9]+$", var.name_prefix))
    error_message = "name_prefix must contain only lowercase letters and numbers."
  }
}

variable "environment" {
  type        = string
  description = "Deployment environment name."
  default     = "dev"
}

variable "location" {
  type        = string
  description = "Azure region for deployed resources."
  default     = "westeurope"
}

variable "tags" {
  type        = map(string)
  description = "Tags applied to all supported resources."
  default = {
    workload = "school-assistant"
  }
}

variable "aspnetcore_environment" {
  type        = string
  description = "ASPNETCORE_ENVIRONMENT value for the backend app."
  default     = "Production"
}

variable "storage_replication_type" {
  type        = string
  description = "Replication type for the storage account."
  default     = "LRS"
}

variable "blob_container_name" {
  type        = string
  description = "Blob container name for source documents."
  default     = "source-documents"
}

variable "cosmosdb_database_name" {
  type        = string
  description = "Cosmos DB SQL database name."
  default     = "school-assistant"
}

variable "cosmosdb_free_tier_enabled" {
  type        = bool
  description = "Enable Cosmos DB free tier when available."
  default     = false
}

variable "search_sku" {
  type        = string
  description = "Azure AI Search SKU."
  default     = "basic"
}

variable "search_replica_count" {
  type        = number
  description = "Replica count for Azure AI Search."
  default     = 1
}

variable "search_partition_count" {
  type        = number
  description = "Partition count for Azure AI Search."
  default     = 1
}

variable "search_index_name" {
  type        = string
  description = "Azure AI Search index name used by the application."
  default     = "school-assistant-index"
}

variable "container_registry_sku" {
  type        = string
  description = "SKU for Azure Container Registry."
  default     = "Basic"
}

variable "backend_image_name" {
  type        = string
  description = "Repository name for the backend image in Azure Container Registry."
  default     = "schoolassistant-api"
}

variable "backend_image_tag" {
  type        = string
  description = "Image tag deployed to the backend container app."
  default     = "latest"
}

variable "backend_container_port" {
  type        = number
  description = "Port exposed by the backend container."
  default     = 8080
}

variable "backend_container_cpu" {
  type        = number
  description = "CPU allocated to the backend container app revision."
  default     = 0.5
}

variable "backend_container_memory" {
  type        = string
  description = "Memory allocated to the backend container app revision."
  default     = "1Gi"
}

variable "container_app_min_replicas" {
  type        = number
  description = "Minimum number of backend container app replicas."
  default     = 0
}

variable "container_app_max_replicas" {
  type        = number
  description = "Maximum number of backend container app replicas."
  default     = 1
}

variable "log_analytics_sku" {
  type        = string
  description = "SKU for Log Analytics workspace."
  default     = "PerGB2018"
}

variable "log_retention_in_days" {
  type        = number
  description = "Retention period for Log Analytics data."
  default     = 30
}

variable "ai_services_sku" {
  type        = string
  description = "SKU for Azure AI Services account."
  default     = "S0"
}

variable "chat_model_deployment_name" {
  type        = string
  description = "Deployment name for the chat model in AI Services."
  default     = "gpt-4o-mini"
}

variable "chat_model_name" {
  type        = string
  description = "OpenAI chat model name."
  default     = "gpt-4o-mini"
}

variable "chat_model_version" {
  type        = string
  description = "OpenAI chat model version."
  default     = "2024-07-18"
}

variable "chat_model_capacity" {
  type        = number
  description = "Token-per-minute capacity (in thousands) for the chat model."
  default     = 10
}

variable "embedding_model_deployment_name" {
  type        = string
  description = "Deployment name for the embedding model in AI Services."
  default     = "text-embedding-3-large"
}

variable "embedding_model_name" {
  type        = string
  description = "OpenAI embedding model name."
  default     = "text-embedding-3-large"
}

variable "embedding_model_version" {
  type        = string
  description = "OpenAI embedding model version."
  default     = "1"
}

variable "embedding_model_capacity" {
  type        = number
  description = "Token-per-minute capacity (in thousands) for the embedding model."
  default     = 10
}

variable "allowed_origins" {
  type        = list(string)
  description = "Allowed origins for the public widget CORS policy."
  default     = ["http://localhost:5173"]
}

variable "allowed_locales" {
  type        = list(string)
  description = "Allowed locales for the public widget."
  default     = ["nl-NL", "en-US", "en-GB"]
}

variable "max_question_length" {
  type        = number
  description = "Maximum accepted user question length."
  default     = 1000
}

variable "rate_limit_permit_limit" {
  type        = number
  description = "Maximum number of chat requests allowed per rate-limit window."
  default     = 20
}

variable "rate_limit_window_seconds" {
  type        = number
  description = "Rate-limit window duration in seconds."
  default     = 60
}

variable "rate_limit_queue_limit" {
  type        = number
  description = "Queue size for the rate limiter."
  default     = 0
}
