# ─── Cluster ────────────────────────────────────────────────────────────────

variable "use_existing_cluster" {
  description = "When true, skip kind cluster creation and connect to an existing cluster via kubeconfig_path/kubeconfig_context."
  type        = bool
  default     = false
}

variable "kind_cluster_name" {
  description = "Name for the kind cluster created by Terraform. Ignored when use_existing_cluster = true."
  type        = string
  default     = "car-repair-shop"
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
  description = "Kubernetes namespace where all resources will be created."
  type        = string
  default     = "car-repair-shop"
}

# ─── Database ───────────────────────────────────────────────────────────────

variable "sa_password" {
  description = "SQL Server SA password. Must meet complexity rules (min 8 chars, upper, lower, digit, special)."
  type        = string
  sensitive   = true
}

variable "mssql_image" {
  description = "SQL Server container image."
  type        = string
  default     = "mcr.microsoft.com/mssql/server:2022-latest"
}

variable "mssql_storage_size" {
  description = "PersistentVolume size requested for SQL Server data."
  type        = string
  default     = "10Gi"
}

# ─── Application ────────────────────────────────────────────────────────────

variable "api_image" {
  description = "Container image for the Car Repair Shop API. Build and push it before running Terraform."
  type        = string
  default     = "car-repair-shop-api:latest"
}

variable "api_replicas" {
  description = "Initial number of API pod replicas."
  type        = number
  default     = 2
}

variable "wait_for_load_balancer" {
  description = "Whether Terraform should wait for the LoadBalancer service to receive an external IP. Set to false for local/CI clusters (e.g. kind) where no cloud load balancer is available."
  type        = bool
  default     = true
}

variable "jwt_secret_key" {
  description = "JWT secret key (minimum 32 characters)."
  type        = string
  sensitive   = true
}

variable "jwt_issuer" {
  description = "JWT issuer claim value."
  type        = string
  default     = "CarRepairShop"
}

variable "jwt_audience" {
  description = "JWT audience claim value."
  type        = string
  default     = "CarRepairShop"
}

variable "jwt_expiration_minutes" {
  description = "JWT token expiration in minutes."
  type        = number
  default     = 60
}

# ─── SMTP ───────────────────────────────────────────────────────────────────

variable "smtp_host" {
  description = "SMTP server hostname."
  type        = string
  default     = "smtp.example.com"
}

variable "smtp_port" {
  description = "SMTP server port."
  type        = number
  default     = 465
}

variable "smtp_use_ssl" {
  description = "Whether to use SSL for SMTP."
  type        = bool
  default     = true
}

variable "smtp_from_email" {
  description = "From address used in outgoing e-mails."
  type        = string
  default     = "no-reply@car-repair-shop.example.com"
}

variable "smtp_from_name" {
  description = "Display name for outgoing e-mails."
  type        = string
  default     = "Car Repair Shop"
}

variable "smtp_username" {
  description = "SMTP authentication username."
  type        = string
  sensitive   = true
  default     = ""
}

variable "smtp_password" {
  description = "SMTP authentication password."
  type        = string
  sensitive   = true
  default     = ""
}

# ─── Application URL ────────────────────────────────────────────────────────

variable "app_base_url" {
  description = "Public base URL of the API (used inside the application)."
  type        = string
  default     = "http://localhost"
}
