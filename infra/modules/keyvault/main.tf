resource "azurerm_key_vault" "this" {
  name                       = var.name
  location                   = var.location
  resource_group_name        = var.resource_group_name
  tenant_id                  = var.tenant_id
  sku_name                   = "standard"
  purge_protection_enabled   = var.purge_protection
  soft_delete_retention_days = 7
  tags                       = var.tags
}

# One read-only policy per consumer identity (e.g. the AKS kubelet identity,
# consumed by the Secrets Store CSI driver).
resource "azurerm_key_vault_access_policy" "readers" {
  for_each = toset(var.reader_object_ids)

  key_vault_id       = azurerm_key_vault.this.id
  tenant_id          = var.tenant_id
  object_id          = each.value
  secret_permissions = ["Get", "List"]
}
