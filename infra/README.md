# OpsForge infrastructure

Reusable Terraform modules composed by thin per-environment roots.

| Module | Provisions |
|---|---|
| `network` | VNet + AKS subnet |
| `acr` | Azure Container Registry (no admin user) |
| `aks` | AKS cluster + AcrPull via kubelet managed identity |
| `keyvault` | Key Vault + per-identity read policies |
| `postgres` | PostgreSQL flexible server + databases |

## Usage

```bash
cd envs/dev            # or envs/staging, envs/prod
export TF_VAR_subscription_id="<sub-id>"
export TF_VAR_postgres_admin_password="<strong-password>"
terraform init
terraform plan
terraform apply
```

State: point each env at an Azure Storage backend before team use (uncomment and
fill the `backend` block; one container, key per environment).

Environment sizing at a glance:

| | dev | staging | prod |
|---|---|---|---|
| AKS nodes | 2 × B2s | 2 × D2s_v3 | 3 × D2s_v3 |
| ACR | Standard | Standard | Premium |
| PostgreSQL | B1ms | GP D2s_v3 | GP D2s_v3 |
| KV purge protection | off | off | **on** |
