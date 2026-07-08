output "namespace" {
  description = "Kubernetes namespace where all resources were deployed."
  value       = kubernetes_namespace.this.metadata[0].name
}

output "api_service_name" {
  description = "Name of the API Kubernetes Service."
  value       = kubernetes_service.api.metadata[0].name
}

output "api_service_type" {
  description = "Type of the API Kubernetes Service (LoadBalancer)."
  value       = kubernetes_service.api.spec[0].type
}

output "mssql_service_name" {
  description = "Name of the MSSQL Kubernetes Service (ClusterIP — internal only)."
  value       = kubernetes_service.mssql.metadata[0].name
}

output "mssql_statefulset_name" {
  description = "Name of the MSSQL StatefulSet."
  value       = kubernetes_stateful_set.mssql.metadata[0].name
}

output "hpa_name" {
  description = "Name of the HorizontalPodAutoscaler for the API."
  value       = kubernetes_horizontal_pod_autoscaler_v2.api.metadata[0].name
}
