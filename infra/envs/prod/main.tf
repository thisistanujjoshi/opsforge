terraform {
  required_version = ">= 1.6.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
  # Shared state lives in Azure Storage in real use — see infra/README.md.
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
}

data "azurerm_client_config" "current" {}

locals {
  env    = "prod"
  prefix = "nexus-${local.env}"
  tags   = { project = "nexuscommerce", environment = local.env, managed_by = "opsforge" }
}

resource "azurerm_resource_group" "main" {
  name     = "${local.prefix}-rg"
  location = var.location
  tags     = local.tags
}

module "network" {
  source              = "../../modules/network"
  name_prefix         = local.prefix
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  tags                = local.tags
}

module "acr" {
  source              = "../../modules/acr"
  name                = replace("${local.prefix}acr", "-", "")
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  sku                 = "Premium"
  tags                = local.tags
}

module "aks" {
  source              = "../../modules/aks"
  name_prefix         = local.prefix
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  subnet_id           = module.network.aks_subnet_id
  acr_id              = module.acr.id
  node_count          = 3
  node_vm_size        = "Standard_D2s_v3"
  tags                = local.tags
}

module "keyvault" {
  source              = "../../modules/keyvault"
  name                = "${local.prefix}-kv"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  tenant_id           = data.azurerm_client_config.current.tenant_id
  reader_object_ids   = [module.aks.kubelet_identity_object_id]
  purge_protection    = true
  tags                = local.tags
}

module "postgres" {
  source              = "../../modules/postgres"
  name_prefix         = local.prefix
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  admin_password      = var.postgres_admin_password
  sku_name            = "GP_Standard_D2s_v3"
  databases           = ["nexus_orders"]
  tags                = local.tags
}
