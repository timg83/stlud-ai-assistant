terraform {
  required_version = ">= 1.7.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

provider "azurerm" {
  features {}
}

locals {
  application_name = "school-ai-assistant"
}

resource "azurerm_resource_group" "main" {
  name     = "rg-${local.application_name}"
  location = var.location
}
