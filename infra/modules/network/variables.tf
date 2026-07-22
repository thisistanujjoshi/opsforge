variable "name_prefix" {
  type        = string
  description = "Prefix for resource names, e.g. nexus-dev."
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "vnet_cidr" {
  type    = string
  default = "10.10.0.0/16"
}

variable "aks_subnet_cidr" {
  type    = string
  default = "10.10.1.0/24"
}

variable "tags" {
  type    = map(string)
  default = {}
}
