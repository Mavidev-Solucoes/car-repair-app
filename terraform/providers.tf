terraform {
  required_version = ">= 1.6.0"

  required_providers {
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.31"
    }
  }
}

# Reads credentials from the local kubeconfig file.
# Make sure the correct context is active before running Terraform:
#   minikube: minikube start  (sets context automatically)
#   kind:     kind create cluster --name car-repair-shop
provider "kubernetes" {
  config_path    = var.kubeconfig_path
  config_context = var.kubeconfig_context
}
