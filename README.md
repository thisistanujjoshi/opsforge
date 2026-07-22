# OpsForge

The platform that NexusCommerce (and later InsightBoard) runs on: Terraform-provisioned
Azure infrastructure, Helm-based Kubernetes deployment, dual CI/CD pipelines with
security scanning, a full observability stack, and a small control-plane API for
deploys and rollbacks.

This repo is also run *as if with a team*: infrastructure decisions are proposed in
[RFCs](docs/rfcs/), every non-trivial change goes through a
[PR checklist](.github/pull_request_template.md), trickier work carries
[pairing notes](docs/pairing-notes/), and each milestone ends with a
[retro](docs/retros/).

## Layout

```
infra/
  modules/          # Reusable Terraform building blocks
    network/        #   VNet + subnets
    aks/            #   AKS cluster + ACR pull identity
    acr/            #   Container registry
    keyvault/       #   Key Vault + access policies
    postgres/       #   Managed PostgreSQL flexible server
  envs/             # Thin per-environment roots composing the modules
    dev/  staging/  prod/
observability/      # Prometheus + Grafana (live), K8s manifests for the full stack
control-plane/      # Deploy/rollback API with audit log
pipelines/          # Azure DevOps + Jenkins mirrors of the GitHub Actions flow
docs/
  rfcs/             # RFC-style write-ups for major infra decisions
  pairing-notes/    # Notes from paired working sessions
  retros/           # Post-milestone retrospectives
```

## Relationship to the app repos

The app repos own their *application* — code, tests, app-specific Helm values.
OpsForge owns the *platform* — cluster infrastructure, shared observability,
deployment tooling, and process. NexusCommerce bootstrapped its own flat Terraform
and pipelines in its Phase 5; [RFC-0001](docs/rfcs/0001-platform-repo-ownership.md)
records the decision to supersede those here as reusable modules.

## Build phases

- [x] **A** — Repo + reusable Terraform modules + process scaffolding
- [ ] **B** — Observability: Prometheus + Grafana against the live NexusCommerce stack
- [ ] **C** — Control-plane API: deploy/rollback with audit log + tests
- [ ] **D** — Pipelines with security scanning + dual-pipeline comparison

## Terraform quick start

```bash
cd infra/envs/dev
export TF_VAR_subscription_id="<sub-id>"
export TF_VAR_postgres_admin_password="<strong-password>"
terraform init && terraform plan
```

Each environment root is ~40 lines composing the same five modules with
different sizing — see `infra/envs/`.
