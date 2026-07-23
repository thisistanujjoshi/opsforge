# Two pipelines, one outcome — GitHub Actions vs Azure DevOps (vs Jenkins)

All three pipelines in this repo implement the same flow:
**validate (terraform fmt/validate + control-plane tests) → security scan (Trivy) → gated deploy.**
Only GitHub Actions actually executes here (it's free for public repos and needs no
server); the other two are faithful mirrors kept runnable for when the
infrastructure exists. Observations from writing all three:

| Concern | GitHub Actions | Azure DevOps | Jenkins |
|---|---|---|---|
| Matrix builds | `strategy.matrix`, terse | `strategy.matrix` per job, wordier | manual `parallel` blocks |
| Approval gates | `environment` protection rules (repo settings) | `environment` on a `deployment` job (portal-configured) | `input` step — in code, but blocks an executor while waiting |
| Marketplace | actions (checkout, setup-*) — huge ecosystem | tasks (UseDotNet@2, HelmDeploy@0) — first-party strong, third-party thinner | plugins — powerful, but version drift is your problem |
| Secrets | repo/environment secrets | variable groups + Key Vault link | credentials store |
| Where config lives | fully in-repo | mostly in-repo, gates/connections in portal | in-repo, but controller state matters |
| Self-hosting | optional runners | optional agents | you own the controller — patching, plugins, backups |

**Calls made here:**

- **Security scanning is report-only initially** (`exit-code: 0`). Gating on a scanner
  you've never run guarantees a red wall of pre-existing findings and teaches the team
  to ignore CI. The pattern: run report-only, triage the baseline, then flip to gating.
  This is deliberate and documented rather than silently "green".
- **The deploy stage calls the control-plane API** (or Helm directly) so the deploy
  path is identical no matter which CI system triggers it — pipelines stay thin,
  the logic lives in one tested place.
- **If choosing one:** GitHub Actions for a repo already on GitHub — the gap that used
  to justify Azure DevOps (environments, approvals, boards) has mostly closed, and
  Jenkins only wins when you need deep customisation or on-prem execution.
