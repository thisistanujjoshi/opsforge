variable "name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "tenant_id" {
  type = string
}

variable "reader_object_ids" {
  type        = list(string)
  description = "Object IDs granted Get/List on secrets."
  default     = []
}

variable "purge_protection" {
  type    = bool
  default = false
}

variable "tags" {
  type    = map(string)
  default = {}
}
