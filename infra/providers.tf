terraform {
  required_version = ">= 1.6.0"

  required_providers {
    kind = {
      source  = "tehcyx/kind"
      version = "~> 0.6"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.31"
    }
  }
}

# ─── Local kind cluster ─────────────────────────────────────────────────────
# Creates a local Kubernetes cluster using kind.
# Set var.use_existing_cluster = true to skip cluster creation and connect
# to a cluster already running (e.g. on a cloud provider).
resource "kind_cluster" "this" {
  count = var.use_existing_cluster ? 0 : 1

  name            = var.kind_cluster_name
  node_image      = var.kind_node_image
  wait_for_ready  = true

  kind_config {
    kind        = "Cluster"
    api_version = "kind.x-k8s.io/v1alpha4"

    node {
      role = "control-plane"
    }

    node {
      role = "worker"
    }
  }
}

# Reads credentials from the local kubeconfig file.
# When a kind cluster is created above, it writes its config to
# ~/.kube/config automatically; the provider will use that context.
provider "kubernetes" {
  config_path    = var.use_existing_cluster ? var.kubeconfig_path : null
  config_context = var.use_existing_cluster ? var.kubeconfig_context : null

  # When kind manages the cluster, consume the generated credentials directly.
  host                   = var.use_existing_cluster ? null : try(kind_cluster.this[0].endpoint, null)
  cluster_ca_certificate = var.use_existing_cluster ? null : try(base64decode(kind_cluster.this[0].cluster_ca_certificate), null)
  client_certificate     = var.use_existing_cluster ? null : try(base64decode(kind_cluster.this[0].client_certificate), null)
  client_key             = var.use_existing_cluster ? null : try(base64decode(kind_cluster.this[0].client_key), null)
}
