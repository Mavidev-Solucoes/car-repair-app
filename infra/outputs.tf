output "kind_cluster_name" {
  description = "Name of the kind cluster created by Terraform (empty when use_existing_cluster = true)."
  value       = var.use_existing_cluster ? "" : try(kind_cluster.this[0].name, "")
}

output "namespace" {
  description = "Kubernetes namespace where all resources were deployed."
  value       = var.namespace
}

output "manifest_namespaces" {
  description = "Namespace manifests applied."
  value       = keys(kubectl_manifest.namespaces)
}

output "manifest_config" {
  description = "Config manifests applied (ConfigMap, Secret)."
  value       = keys(kubectl_manifest.config)
}

output "manifest_app" {
  description = "Application manifests applied (Database, API, HPA)."
  value       = keys(kubectl_manifest.app)
}
