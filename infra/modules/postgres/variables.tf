variable "name_prefix" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "admin_login" {
  type    = string
  default = "nexusadmin"
}

variable "admin_password" {
  type      = string
  sensitive = true
}

variable "sku_name" {
  type    = string
  default = "B_Standard_B1ms"
}

variable "storage_mb" {
  type    = number
  default = 32768
}

variable "databases" {
  type        = list(string)
  description = "Databases to create on the server."
  default     = []
}

variable "tags" {
  type    = map(string)
  default = {}
}
