variable "subscription_id" {
  type        = string
  description = "Azure subscription — supply via TF_VAR_subscription_id."
  default     = "00000000-0000-0000-0000-000000000000"
}

variable "location" {
  type    = string
  default = "westeurope"
}

variable "postgres_admin_password" {
  type        = string
  sensitive   = true
  description = "Supply via TF_VAR_postgres_admin_password — never commit."
  default     = null
}
