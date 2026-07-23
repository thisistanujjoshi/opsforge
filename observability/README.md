# OpsForge observability

Prometheus + Grafana scraping the NexusCommerce platform's `/metrics` endpoints
(added to every service: `prometheus-net` on the .NET APIs,
`prometheus-fastapi-instrumentator` on the Python ones — both emit the shared
`http_request_duration_seconds` histogram, so latency panels work across stacks).

```bash
docker compose up -d
# Grafana:    http://localhost:3000  (anonymous viewer; admin/admin to edit)
# Dashboard:  http://localhost:3000/d/nexus-health
# Prometheus: http://localhost:9090/targets
```

The provisioned **NexusCommerce — Service Health** dashboard shows per-service
up status, request rate, p95 latency, 5xx rate, and process memory.

## Kubernetes

In-cluster, this role is filled by the standard charts rather than hand-rolled
manifests — `kube-prometheus-stack` (Prometheus operator + Grafana + node/kube
metrics) with Loki + Promtail for logs and the OpenTelemetry collector for
traces. The services' `/metrics` endpoints are discovered via a `ServiceMonitor`
selecting the `app.kubernetes.io/part-of: nexuscommerce` label that the
NexusCommerce Helm chart already applies. This compose file exists so the same
dashboards work on a laptop without a cluster.
