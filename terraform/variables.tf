# ─── Cluster ────────────────────────────────────────────────────────────────

variable "kubeconfig_path" {
  description = "Absolute path to the kubeconfig file used to reach the local cluster."
  type        = string
  default     = "~/.kube/config"
}

variable "kubeconfig_context" {
  description = "kubeconfig context to use. Leave empty to use the current active context."
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
