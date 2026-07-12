# ═══════════════════════════════════════════════════════════════════════════
# Namespace
# ═══════════════════════════════════════════════════════════════════════════

resource "kubernetes_namespace" "this" {
  metadata {
    name = var.namespace
    labels = {
      app = var.namespace
    }
  }
}

# ═══════════════════════════════════════════════════════════════════════════
# ConfigMap — non-sensitive configuration shared by all workloads
# ═══════════════════════════════════════════════════════════════════════════

resource "kubernetes_config_map" "app_config" {
  metadata {
    name      = "car-repair-shop-config"
    namespace = kubernetes_namespace.this.metadata[0].name
  }

  data = {
    # ASP.NET Core
    ASPNETCORE_ENVIRONMENT  = "Production"
    ASPNETCORE_HTTP_PORTS   = "8080"

    # JWT (non-sensitive fields only)
    "JwtSettings__Issuer"             = var.jwt_issuer
    "JwtSettings__Audience"           = var.jwt_audience
    "JwtSettings__ExpirationMinutes"  = tostring(var.jwt_expiration_minutes)

    # Application
    "AppSettings__BaseUrl" = var.app_base_url

    # SMTP (non-sensitive fields)
    "SmtpSettings__Host"      = var.smtp_host
    "SmtpSettings__Port"      = tostring(var.smtp_port)
    "SmtpSettings__UseSsl"    = tostring(var.smtp_use_ssl)
    "SmtpSettings__FromEmail" = var.smtp_from_email
    "SmtpSettings__FromName"  = var.smtp_from_name

    # SQL Server (used by the MSSQL StatefulSet)
    ACCEPT_EULA = "Y"
    MSSQL_PID   = "Developer"
  }
}

# ═══════════════════════════════════════════════════════════════════════════
# Secret — sensitive credentials
# ═══════════════════════════════════════════════════════════════════════════

resource "kubernetes_secret" "app_secrets" {
  metadata {
    name      = "car-repair-shop-secrets"
    namespace = kubernetes_namespace.this.metadata[0].name
  }

  type = "Opaque"

  data = {
    SA_PASSWORD    = var.sa_password
    JWT_SECRET_KEY = var.jwt_secret_key

    # Connection string — Password matches SA_PASSWORD variable
    "ConnectionStrings__DefaultConnection" = "Server=mssql;Database=CarRepairShopDb;User Id=sa;Password=${var.sa_password};TrustServerCertificate=True;"

    # SMTP credentials
    "SmtpSettings__Username" = var.smtp_username
    "SmtpSettings__Password" = var.smtp_password
  }
}

# ═══════════════════════════════════════════════════════════════════════════
# Database — SQL Server 2022 StatefulSet + ClusterIP Service
# ═══════════════════════════════════════════════════════════════════════════

resource "kubernetes_stateful_set" "mssql" {
  metadata {
    name      = "mssql"
    namespace = kubernetes_namespace.this.metadata[0].name
    labels = {
      app = "mssql"
    }
  }

  spec {
    service_name = "mssql"
    replicas     = 1

    selector {
      match_labels = {
        app = "mssql"
      }
    }

    template {
      metadata {
        labels = {
          app = "mssql"
        }
      }

      spec {
        container {
          name  = "mssql"
          image = var.mssql_image

          port {
            container_port = 1433
          }

          env {
            name = "ACCEPT_EULA"
            value_from {
              config_map_key_ref {
                name = kubernetes_config_map.app_config.metadata[0].name
                key  = "ACCEPT_EULA"
              }
            }
          }

          env {
            name = "MSSQL_PID"
            value_from {
              config_map_key_ref {
                name = kubernetes_config_map.app_config.metadata[0].name
                key  = "MSSQL_PID"
              }
            }
          }

          env {
            name = "SA_PASSWORD"
            value_from {
              secret_key_ref {
                name = kubernetes_secret.app_secrets.metadata[0].name
                key  = "SA_PASSWORD"
              }
            }
          }

          volume_mount {
            name       = "mssql-data"
            mount_path = "/var/opt/mssql"
          }

          resources {
            requests = {
              cpu    = "500m"
              memory = "1Gi"
            }
            limits = {
              cpu    = "2"
              memory = "2Gi"
            }
          }

          liveness_probe {
            exec {
              command = [
                "/bin/sh", "-c",
                "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$SA_PASSWORD\" -Q \"SELECT 1\" -No",
              ]
            }
            initial_delay_seconds = 60
            period_seconds        = 10
            timeout_seconds       = 5
            failure_threshold     = 3
          }

          readiness_probe {
            exec {
              command = [
                "/bin/sh", "-c",
                "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$SA_PASSWORD\" -Q \"SELECT 1\" -No",
              ]
            }
            initial_delay_seconds = 30
            period_seconds        = 10
            timeout_seconds       = 5
            failure_threshold     = 6
          }
        }
      }
    }

    volume_claim_template {
      metadata {
        name = "mssql-data"
      }
      spec {
        access_modes = ["ReadWriteOnce"]
        resources {
          requests = {
            storage = var.mssql_storage_size
          }
        }
      }
    }
  }

  wait_for_rollout = false
}

resource "kubernetes_service" "mssql" {
  metadata {
    name      = "mssql"
    namespace = kubernetes_namespace.this.metadata[0].name
    labels = {
      app = "mssql"
    }
  }

  spec {
    selector = {
      app = "mssql"
    }

    port {
      name        = "mssql"
      port        = 1433
      target_port = 1433
    }

    type = "ClusterIP"
  }
}

# ═══════════════════════════════════════════════════════════════════════════
# API — Deployment + LoadBalancer Service
# ═══════════════════════════════════════════════════════════════════════════

resource "kubernetes_deployment" "api" {
  metadata {
    name      = "car-repair-shop-api"
    namespace = kubernetes_namespace.this.metadata[0].name
    labels = {
      app = "car-repair-shop-api"
    }
  }

  spec {
    replicas = var.api_replicas

    selector {
      match_labels = {
        app = "car-repair-shop-api"
      }
    }

    template {
      metadata {
        labels = {
          app = "car-repair-shop-api"
        }
      }

      spec {
        container {
          name              = "car-repair-shop-api"
          image             = var.api_image
          image_pull_policy = "IfNotPresent"

          port {
            container_port = 8080
          }

          env_from {
            config_map_ref {
              name = kubernetes_config_map.app_config.metadata[0].name
            }
          }

          env_from {
            secret_ref {
              name = kubernetes_secret.app_secrets.metadata[0].name
            }
          }

          resources {
            requests = {
              cpu    = "250m"
              memory = "256Mi"
            }
            limits = {
              cpu    = "500m"
              memory = "512Mi"
            }
          }

          liveness_probe {
            http_get {
              path = "/health"
              port = 8080
            }
            initial_delay_seconds = 30
            period_seconds        = 10
            timeout_seconds       = 5
            failure_threshold     = 3
          }

          readiness_probe {
            http_get {
              path = "/health"
              port = 8080
            }
            initial_delay_seconds = 15
            period_seconds        = 10
            timeout_seconds       = 5
            failure_threshold     = 3
          }
        }
      }
    }
  }

  wait_for_rollout = false
}

resource "kubernetes_service" "api" {
  metadata {
    name      = "car-repair-shop-api"
    namespace = kubernetes_namespace.this.metadata[0].name
    labels = {
      app = "car-repair-shop-api"
    }
  }

  spec {
    selector = {
      app = "car-repair-shop-api"
    }

    port {
      name        = "http"
      port        = 80
      target_port = 8080
    }

    type = "LoadBalancer"
  }

  wait_for_load_balancer = var.wait_for_load_balancer
}

# ═══════════════════════════════════════════════════════════════════════════
# HorizontalPodAutoscaler — API
# ═══════════════════════════════════════════════════════════════════════════

resource "kubernetes_horizontal_pod_autoscaler_v2" "api" {
  metadata {
    name      = "car-repair-shop-api-hpa"
    namespace = kubernetes_namespace.this.metadata[0].name
  }

  spec {
    scale_target_ref {
      api_version = "apps/v1"
      kind        = "Deployment"
      name        = kubernetes_deployment.api.metadata[0].name
    }

    min_replicas = 2
    max_replicas = 5

    metric {
      type = "Resource"
      resource {
        name = "cpu"
        target {
          type                = "Utilization"
          average_utilization = 70
        }
      }
    }

    metric {
      type = "Resource"
      resource {
        name = "memory"
        target {
          type                = "Utilization"
          average_utilization = 80
        }
      }
    }
  }
}
