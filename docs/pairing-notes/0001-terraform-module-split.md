# Pairing note 0001 — Terraform module split (2026-07-22)

**Context:** restructuring NexusCommerce's flat Terraform into OpsForge's
modules + env roots (RFC-0001).

## What we worked through

- **Module boundaries.** First cut had one `cluster` module owning AKS + ACR +
  the role assignment. Split ACR out: the registry outlives any one cluster, and
  the AcrPull role assignment moved *into* the AKS module because it describes the
  cluster's identity, not the registry.
- **Key Vault access.** Inline `access_policy` blocks inside `azurerm_key_vault`
  fight with `for_each` consumers — switched to separate
  `azurerm_key_vault_access_policy` resources keyed by object ID, so adding a
  reader identity is additive rather than a rewrite of the vault resource.
- **Env divergence.** Resisted the urge to parameterise everything; the env roots
  intentionally repeat module calls with literal sizing. A reviewer can answer
  "what's different in prod?" by diffing two ~80-line files.

## Dead ends

- Tried validating locally; the azurerm provider's gRPC handshake is broken by
  AV TLS interception on this machine (`x509: certificate signed by unknown
  authority` on loopback). Wasted ~20 minutes before reading the plugin log.
  Lesson recorded in RFC-0001: validation belongs in CI here.

## Follow-ups

- [ ] Slim NexusCommerce's `deploy/terraform` to a pointer at OpsForge
- [ ] Add InsightBoard's database + Key Vault entries when project 3 starts
