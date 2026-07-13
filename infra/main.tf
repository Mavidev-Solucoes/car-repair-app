# ═══════════════════════════════════════════════════════════════════════════
# Manifest directories
# ═══════════════════════════════════════════════════════════════════════════

locals {
  namespace_files = fileset("${path.module}/manifests/00-namespaces", "*.yaml")
  config_files    = fileset("${path.module}/manifests/01-config", "*.yaml")
  app_files       = fileset("${path.module}/manifests/02-app", "*.yaml")

  # Variables injected into every manifest template.
  template_vars = {
    namespace              = var.namespace
    # Database
    mssql_image            = var.mssql_image
    mssql_storage_size     = var.mssql_storage_size
    # Application
    api_image              = var.api_image
    api_replicas           = var.api_replicas
    # JWT
    jwt_issuer             = var.jwt_issuer
    jwt_audience           = var.jwt_audience
    jwt_expiration_minutes = var.jwt_expiration_minutes
    # Application URL
    app_base_url           = var.app_base_url
    # SMTP (non-sensitive)
    smtp_host              = var.smtp_host
    smtp_port              = var.smtp_port
    smtp_use_ssl           = var.smtp_use_ssl
    smtp_from_email        = var.smtp_from_email
    smtp_from_name         = var.smtp_from_name
    # Secrets
    sa_password            = var.sa_password
    jwt_secret_key         = var.jwt_secret_key
    smtp_username          = var.smtp_username
    smtp_password          = var.smtp_password
  }
}

# ═══════════════════════════════════════════════════════════════════════════
# Step 1 — Namespaces
# ═══════════════════════════════════════════════════════════════════════════

resource "kubectl_manifest" "namespaces" {
  for_each  = local.namespace_files
  yaml_body = templatefile("${path.module}/manifests/00-namespaces/${each.value}", local.template_vars)
}

# ═══════════════════════════════════════════════════════════════════════════
# Step 2 — ConfigMap and Secret
# ═══════════════════════════════════════════════════════════════════════════

resource "kubectl_manifest" "config" {
  for_each  = local.config_files
  yaml_body = templatefile("${path.module}/manifests/01-config/${each.value}", local.template_vars)

  sensitive_fields = [
    "stringData.SA_PASSWORD",
    "stringData.JWT_SECRET_KEY",
    "stringData.ConnectionStrings__DefaultConnection",
    "stringData.SmtpSettings__Username",
    "stringData.SmtpSettings__Password",
  ]

  depends_on = [kubectl_manifest.namespaces]
}

# ═══════════════════════════════════════════════════════════════════════════
# Step 3 — Database, Application and HPA
# ═══════════════════════════════════════════════════════════════════════════

resource "kubectl_manifest" "app" {
  for_each  = local.app_files
  yaml_body = templatefile("${path.module}/manifests/02-app/${each.value}", local.template_vars)

  depends_on = [kubectl_manifest.config]
}
