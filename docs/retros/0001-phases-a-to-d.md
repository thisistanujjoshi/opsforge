# Retro — OpsForge phases A–D (2026-07-23)

## What went well

- **Module split paid off immediately**: the env roots came out at ~80 lines and the
  dev/staging/prod diff is readable at a glance — exactly the review property RFC-0001
  aimed for.
- **CI as the escape hatch**: local `terraform validate` is impossible on this machine
  (AV TLS interception); moving validation to GitHub runners turned a blocker into a
  green matrix job on the first push.
- **Cross-stack metrics convergence**: choosing exporters that share the
  `http_request_duration_seconds` histogram meant one PromQL query covers .NET and
  Python services — dashboard stayed simple.
- **Control plane first-time green**: 8/8 tests on first build; the stub-executor
  pattern (borrowed from the app repo's provider switches) made the API trivially
  testable.

## What was painful

- **Headless screenshots of Grafana** never rendered (SPA + headless Edge). Burned
  several attempts; verification moved to Grafana's query API instead, which was the
  stronger proof anyway. Lesson: verify data pipelines at the API, not the pixels.
- **Machine limits**: 8 GB RAM means the full stack + observability + builds contend;
  background builds while writing config was the workable rhythm.

## What we'd change next time

- Add the ServiceMonitor labels to the app Helm chart *when the chart is written*,
  not retroactively — observability requirements belong in the definition of done.
- Start the security scan report-only from day one (done here) but schedule the
  "flip to gating" as a dated follow-up, not a vague intention.

## Follow-ups

- [ ] Slim NexusCommerce `deploy/terraform` to a pointer at OpsForge (RFC-0001)
- [ ] Triage Trivy baseline → flip `exit-code: 1`
- [ ] Wire the control-plane's Helm executor against a real cluster when one exists
