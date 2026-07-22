# What & why

<!-- One or two sentences: what changes, and what problem it solves. Link the RFC if one exists. -->

# Self-review checklist

- [ ] `terraform fmt -check` / linters pass locally
- [ ] Plan output reviewed — no unexpected destroys or replacements
- [ ] No secrets, connection strings, or real subscription IDs in the diff
- [ ] Sizing/SKU changes are per-environment, not hardcoded in a module
- [ ] Docs updated (README, RFC, or runbook) if behaviour or layout changed
- [ ] Rollback path considered — how do we undo this if it goes wrong?

# Evidence

<!-- Paste the relevant plan/lint/test output or a screenshot. -->

# Reviewer notes

<!-- Anything you want a second pair of eyes on specifically. -->
