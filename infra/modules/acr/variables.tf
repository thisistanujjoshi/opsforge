variable "name" {
  type        = string
  description = "Registry name — alphanumeric, globally unique."

  validation {
    condition     = can(regex("^[a-zA-Z0-9]+$", var.name))
    error_message = "ACR names must be alphanumeric."
  }
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "sku" {
  type    = string
  default = "Standard"
}

variable "tags" {
  type    = map(string)
  default = {}
}
