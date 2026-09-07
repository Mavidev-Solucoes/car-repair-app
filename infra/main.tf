# ═══════════════════════════════════════════════════════════════════════════
# Manifest directories
# ═══════════════════════════════════════════════════════════════════════════

locals {
  namespace_files = fileset("${path.module}/manifests/00-namespaces", "*.yaml")
  config_files    = fileset("${path.module}/manifests/01-config", "*.yaml")
  app_files       = fileset("${path.module}/manifests/02-app", "*.yaml")

  template_vars = {
    namespace                        = var.namespace
    api_image                        = var.api_image
    api_replicas                     = var.api_replicas
    aws_region                       = var.aws_region
    irsa_role_arn                    = var.irsa_role_arn
    external_secret_refresh_interval = var.external_secret_refresh_interval
    jwt_secret_manager_secret_id     = var.jwt_secret_manager_secret_id
    postgres_connection_secret_id    = var.postgres_connection_secret_id
    smtp_settings_secret_id          = var.smtp_settings_secret_id
    jwt_issuer                       = var.jwt_issuer
    jwt_audience                     = var.jwt_audience
    jwt_expiration_minutes           = var.jwt_expiration_minutes
    app_base_url                     = var.app_base_url
  }
}

# ═══════════════════════════════════════════════════════════════════════════
# Step 1 — Namespace
# ═══════════════════════════════════════════════════════════════════════════

resource "kubectl_manifest" "namespaces" {
  for_each  = local.namespace_files
  yaml_body = templatefile("${path.module}/manifests/00-namespaces/${each.value}", local.template_vars)
}

# ═══════════════════════════════════════════════════════════════════════════
# Step 2 — ServiceAccount + External Secrets resources
# ═══════════════════════════════════════════════════════════════════════════

resource "kubectl_manifest" "config" {
  for_each  = local.config_files
  yaml_body = templatefile("${path.module}/manifests/01-config/${each.value}", local.template_vars)

  depends_on = [kubectl_manifest.namespaces]
}

# ═══════════════════════════════════════════════════════════════════════════
# Step 3 — API resources
# ═══════════════════════════════════════════════════════════════════════════

resource "kubectl_manifest" "app" {
  for_each  = local.app_files
  yaml_body = templatefile("${path.module}/manifests/02-app/${each.value}", local.template_vars)

  depends_on = [kubectl_manifest.config]
}
