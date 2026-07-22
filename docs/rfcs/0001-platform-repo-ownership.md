# RFC-0001 — Platform repo ownership and the module split

- **Status:** Accepted
- **Author:** Tanuj Joshi
- **Date:** 2026-07-22

## Problem

NexusCommerce bootstrapped its own infrastructure in its Phase 5: a flat Terraform
configuration, a Helm chart, and CI/CD pipelines, all living inside the app repo.
That was the right sequencing call (ship the app first), but it doesn't scale to a
second application: InsightBoard would either duplicate the Terraform or reach into
another app's repo for shared infrastructure. Ownership is also wrong — cluster-level
concerns (networking, registry, observability) don't belong to one application team.

## Decision

1. **OpsForge is the platform repo.** It owns: Azure infrastructure (as reusable
   Terraform modules), the shared observability stack, deployment tooling (the
   control-plane API), pipeline templates with security scanning, and engineering
   process (RFCs, PR checklist, pairing notes, retros).

2. **Terraform is restructured from a flat config into modules + thin env roots.**
   `infra/modules/{network,acr,aks,keyvault,postgres}` are reusable building blocks
   with explicit variables/outputs; `infra/envs/{dev,staging,prod}` are ~80-line
   roots that compose them with per-environment sizing. Adding InsightBoard's
   database later is a one-line `databases` list change, not a copy-paste.

3. **App repos keep their app charts and values.** NexusCommerce's Helm chart stays
   in the NexusCommerce repo — it describes the *application*. Its `deploy/terraform`
   flat config is superseded by OpsForge and will be slimmed to a pointer in a
   follow-up PR to avoid two sources of truth.

## Alternatives considered

- **Keep everything in each app repo.** Fine for one app; duplicates from the second
  app onward, and platform concerns get PR-reviewed by the wrong owners.
- **A Terraform monorepo with workspaces instead of env directories.** Workspaces
  hide the environment in state rather than in code; directory-per-env keeps diffs
  reviewable ("what changes in prod?" is a file diff, not a workspace flag).
- **Terragrunt.** Solves the same DRY problem with an extra tool; module + thin-root
  achieves it with vanilla Terraform at this scale.

## Consequences

- Two repos must stay compatible at one seam: the Helm `image.registry` /
  Key Vault names that OpsForge outputs and NexusCommerce's chart consumes.
  The env roots expose exactly those outputs.
- `terraform validate`/`apply` cannot run on the current dev machine (antivirus TLS
  interception breaks the terraform↔provider handshake — see pairing note 0001).
  Formatting and review discipline still apply; validation runs in CI on GitHub's
  runners where no interception exists.
