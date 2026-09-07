# ─── Cluster ────────────────────────────────────────────────────────────────

variable "use_existing_cluster" {
  description = "When true, skip kind cluster creation and connect to an existing cluster via kubeconfig_path/kubeconfig_context."
  type        = bool
  default     = false
}

variable "kind_cluster_name" {
  description = "Name for the kind cluster created by Terraform. Ignored when use_existing_cluster = true."
  type        = string
  default     = "car-repair-app"
}

variable "kind_node_image" {
  description = "kind node image. Pin to a specific Kubernetes version for reproducibility."
  type        = string
  default     = "kindest/node:v1.31.0"
}

variable "kubeconfig_path" {
  description = "Absolute path to the kubeconfig file. Used only when use_existing_cluster = true."
  type        = string
  default     = "~/.kube/config"
}

variable "kubeconfig_context" {
  description = "kubeconfig context to use. Used only when use_existing_cluster = true. Leave empty for the active context."
  type        = string
  default     = ""
}

# ─── Namespace ──────────────────────────────────────────────────────────────

variable "namespace" {
  description = "Kubernetes namespace where application resources will be created."
  type        = string
  default     = "car-repair-app"
}

# ─── Application ────────────────────────────────────────────────────────────

variable "api_image" {
  description = "Container image for the Car Repair App API. Build and push it before running Terraform."
  type        = string
  default     = "car-repair-app-api:latest"
}

variable "api_replicas" {
  description = "Initial number of API pod replicas."
  type        = number
  default     = 2
}

# ─── External Secrets / AWS ────────────────────────────────────────────────

variable "aws_region" {
  description = "AWS region where Secrets Manager secrets are stored."
  type        = string
  default     = "us-east-1"
}

variable "irsa_role_arn" {
  description = "IAM Role ARN to bind to the application ServiceAccount via IRSA."
  type        = string
}

variable "external_secret_refresh_interval" {
  description = "Refresh interval used by External Secrets when syncing from AWS Secrets Manager."
  type        = string
  default     = "1h"
}

variable "jwt_secret_manager_secret_id" {
  description = "AWS Secrets Manager secret id/name/ARN that stores the JWT secret."
  type        = string
}

variable "postgres_connection_secret_id" {
  description = "AWS Secrets Manager secret id/name/ARN that stores PostgreSQL connection settings."
  type        = string
}

variable "smtp_settings_secret_id" {
  description = "AWS Secrets Manager secret id/name/ARN that stores SMTP settings."
  type        = string
}

# ─── Application URL ────────────────────────────────────────────────────────

variable "app_base_url" {
  description = "Public base URL of the API (used inside the application)."
  type        = string
  default     = "http://localhost"
}

# ─── JWT metadata ───────────────────────────────────────────────────────────

variable "jwt_issuer" {
  description = "JWT issuer claim value."
  type        = string
  default     = "car-repair-auth"
}

variable "jwt_audience" {
  description = "JWT audience claim value."
  type        = string
  default     = "car-repair-app"
}

variable "jwt_expiration_minutes" {
  description = "JWT token expiration in minutes."
  type        = number
  default     = 60
}
