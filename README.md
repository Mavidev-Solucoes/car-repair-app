# car-repair-app

API .NET 8 do Car Repair para gestao de clientes, veiculos, ordens de servico, catalogo de servicos e pecas.

Este repositorio contem a aplicacao, Dockerfile, testes e manifests Kubernetes do workload. A infraestrutura base fica em repositorios separados.

## Tecnologias e papel no sistema

- **ASP.NET Core (.NET 8)**: framework da API REST (pipeline HTTP, controllers, middlewares, autenticacao/autorizacao e health checks).
- **Entity Framework Core**: ORM para mapeamento das entidades e persistencia; migrations aplicadas por job dedicado com `--migrate`.
- **PostgreSQL**: banco relacional principal da aplicacao.
- **Kong Gateway**: camada de borda para roteamento, JWT plugin, CORS, rate limiting e correlation ID.
- **JWT**: padrao de autenticacao da API e do gateway (`iss=car-repair-auth`, `aud=car-repair-shop`).
- **AWS Secrets Manager**: origem dos segredos por ambiente (`database`, `jwt`, `newrelic`, `smtp`), sincronizados via External Secrets Operator.
- **New Relic**: APM e telemetria de negocio com eventos customizados.

## Dependencias externas

`car-repair-k8s-infra` fornece:

- Amazon EKS
- namespace `car-repair-app`
- ECR `car-repair-app`
- External Secrets Operator
- Metrics Server
- Cluster Autoscaler
- AWS Load Balancer Controller

`car-repair-db-infra` fornece:

- RDS PostgreSQL privado
- secret `car-repair/<environment>/database`

`car-repair-auth-lambda` fornece:

- secret `car-repair/<environment>/jwt`
- emissor JWT `car-repair-auth`
- audience JWT `car-repair-shop`
- endpoint HTTP usado por `AuthLambda__BaseUrl`, publicado pela arquitetura de gateway do deploy `car-repair-auth-lambda`

Antes de executar deploy, os secrets abaixo devem existir no AWS Secrets Manager para o ambiente alvo:

- `car-repair/<environment>/database`
- `car-repair/<environment>/jwt`
- `car-repair/<environment>/newrelic`
- `car-repair/<environment>/smtp`

O secret `car-repair/<environment>/smtp` deve conter:

```json
{
  "username": "...",
  "password": "..."
}
```

Este repositorio nao cria EKS, RDS, ECR, Kong, New Relic, RDS Proxy ou Ingress.
Ele apenas declara os recursos da aplicacao, incluindo as rotas e policies Kong
especificas do `car-repair-app`.

## Fluxo

Diagrama da arquitetura atual:

![Arquitetura car-repair-app](https://github.com/user-attachments/assets/cf156351-fa45-4982-9587-57aaf182a4ea)

```text
ECR
 |
 v
EKS
 |
 v
Car Repair API
 |
 v
RDS PostgreSQL
```

Entrada HTTP publica:

```text
Internet
   |
  NLB
   |
 Kong
   |
 +--------------------+
 |                    |
public              protected
 |                    |
login              JWT Plugin
 |                    |
 +------ car-repair-app
              |
       role authorization
```

Secrets:

```text
Secrets Manager
 ├── database
 ├── jwt
 ├── newrelic
 └── smtp
       ↓
External Secrets Operator
       ↓
car-repair-app-secrets
```

## Estrategia de secrets

Foi adotada a estrategia preferencial: o External Secrets Operator sincroniza os secrets do AWS Secrets Manager para um Kubernetes Secret, e a API recebe os valores por variaveis de ambiente.

A aplicacao nao chama AWS Secrets Manager diretamente e nao precisa de IRSA propria. O External Secrets Operator usa sua propria ServiceAccount/IRSA, portanto desabilitar o token da ServiceAccount da aplicacao nao afeta a sincronizacao de secrets.

Secrets consumidos:

- `car-repair/<environment>/database`
- `car-repair/<environment>/jwt`
- `car-repair/<environment>/newrelic`
- `car-repair/<environment>/smtp`

O `ExternalSecret` gera o Kubernetes Secret `car-repair-app-secrets` com:

- `ConnectionStrings__DefaultConnection`
- `JwtSettings__SecretKey`
- `NEW_RELIC_LICENSE_KEY`
- `SmtpSettings__Username`
- `SmtpSettings__Password`

Nenhuma credencial AWS e colocada nos manifests.

O Kong tambem precisa validar a assinatura HS256 emitida pelo
`car-repair-auth-lambda`. Para isso, `k8s/base/gateway` cria um
`ExternalSecret` separado que reutiliza `car-repair/<environment>/jwt` e gera o
Secret Kubernetes `kong-jwt-credential-car-repair-auth` no formato esperado pelo
Kong Ingress Controller:

- label `konghq.com/credential: jwt`
- `key=car-repair-auth`
- `algorithm=HS256`
- `secret=<valor vindo do Secrets Manager>`

O signing secret nao e duplicado em YAML. O `KongConsumer` `car-repair-auth`
referencia esse Secret como credential JWT, e o JWT plugin usa `iss` como
`key_claim_name`. A API continua validando o mesmo JWT via JwtBearer.

## Observabilidade de negocio

A aplicacao emite eventos customizados de negocio via New Relic .NET Agent API.
Os eventos nao incluem CPF, nome, email, telefone, endereco, JWT ou secrets.

### `CarRepairServiceOrderCreated`

Emitido depois que uma ordem de servico e criada e persistida com sucesso.

Atributos:

- `ServiceOrderId`
- `Status`
- `CreatedAt`
- `CorrelationId`
- `Environment`

Consulta conceitual para ordens por dia:

```sql
FROM CarRepairServiceOrderCreated
SELECT count(*)
TIMESERIES 1 day
```

### `CarRepairServiceOrderStatusChanged`

Emitido depois que uma transicao valida de status da ordem de servico e
persistida com sucesso.

Atributos:

- `ServiceOrderId`
- `PreviousStatus`
- `NewStatus`
- `ChangedAt`
- `CorrelationId`
- `Environment`
- `DurationSeconds`, quando calculavel pelo historico persistido

`DurationSeconds` representa o tempo em segundos entre a entrada da ordem no
`PreviousStatus` e a transicao seguinte para `NewStatus`, usando timestamps UTC
do historico de status da propria ordem.

Consulta conceitual para tempo medio por status:

```sql
FROM CarRepairServiceOrderStatusChanged
SELECT average(DurationSeconds)
FACET PreviousStatus
```

## Kubernetes

Manifests:

```text
k8s/
  base/
    prerequisites/
      configmap.yaml
      serviceaccount.yaml
      secretstore.yaml
      externalsecret.yaml
    migration/
      migration-job.yaml
    workload/
      deployment.yaml
      service.yaml
      hpa.yaml
    gateway/
      kong-plugins.yaml
      kong-consumer.yaml
      kong-jwt-credential-external-secret.yaml
      kong-public-ingress.yaml
      kong-protected-ingress.yaml
  overlays/
    dev/
      prerequisites/
      migration/
      workload/
      gateway/
    prod/
      prerequisites/
      migration/
      workload/
      gateway/
```

Recursos principais:

- `Deployment` `car-repair-app`, 2 replicas por padrao
- `ServiceAccount` dedicada `car-repair-app`, com `automountServiceAccountToken: false`
- `Service` `ClusterIP`, porta `80` -> `8080`
- `HorizontalPodAutoscaler` autoscaling/v2, min 2, max 5, CPU 70%, memoria 80%
- `Job` `car-repair-app-migration` para EF Core migrations
- `ExternalSecret` para database/JWT/SMTP
- `Ingress` Kong publico para `/api/auth/login`
- `Ingress` Kong publico para callbacks anonimos de aprovacao/rejeicao de servico
- `Ingress` Kong protegido para `/api`
- `KongPlugin` para JWT, rate limiting, CORS e correlation ID
- `KongConsumer` para o emissor `car-repair-auth`

No Kubernetes, a saude do container e controlada pelo proprio `Deployment` usando:

- `readinessProbe` em `/health`
- `livenessProbe` em `/health`

Nao ha Service `LoadBalancer` direto da API. A entrada externa deve ser via Kong,
instalado pelo repositorio de infraestrutura.

## Kong Gateway

O repositorio nao instala Kong. O `car-repair-k8s-infra` fornece Kong Gateway,
Kong Ingress Controller, `IngressClass` `kong`, NLB publico e modo DB-less.
Este repositorio adiciona apenas os recursos especificos da aplicacao.

Responsabilidades:

- Kong: autenticacao de borda e policies transversais.
- API: nova validacao JwtBearer, autorizacao por role e regras de negocio.

Rotas publicas:

- `POST /api/auth/login`
- `PATCH /api/services/{id}/approve`
- `PATCH /api/services/{id}/reject`

Rotas protegidas:

- catch-all `/api` em Ingress separado, com JWT plugin.

As rotas publicas nao recebem o JWT plugin. O catch-all protegido nao e aplicado
no Service globalmente para evitar que login e callbacks anonimos herdem
autenticacao indevidamente. As rotas publicas especificas devem ter prioridade
sobre `/api`.

Plugins:

- JWT: valida token presente, assinatura HS256, `iss=car-repair-auth` e `exp`.
- Rate limiting login: `5` requisicoes por minuto, policy `local`.
- Rate limiting API: `100` requisicoes por minuto, policy `local`.
- CORS: metodos `GET`, `POST`, `PUT`, `PATCH`, `DELETE`, `OPTIONS`; headers
  `Authorization`, `Content-Type`, `X-Correlation-ID`.
- Correlation ID: usa `X-Correlation-ID`, ecoa o header para o cliente e
  preserva o valor recebido quando fornecido; quando ausente, Kong gera um UUID.

Rate limiting usa policy `local`, compativel com Kong DB-less e sem Redis nesta
etapa. Em ambientes com multiplas replicas de Kong, o limite e aplicado por
instancia.

CORS e parametrizado nos overlays:

- dev: `origins=["*"]`, `credentials=false`.
- prod: origem explicita placeholder `https://app.car-repair.example.com`,
  `credentials=false`. Ajuste para o dominio real antes do deploy produtivo.
  Se `credentials=true` for habilitado no futuro, nao use `*`.

Path handling:

- todos os Ingresses usam `konghq.com/strip-path: "false"`.
- Kong encaminha `/api/...` exatamente como a API espera.
- `/health` nao e exposto por Ingress; readiness/liveness continuam internas ao
  Kubernetes.

## ConfigMap

O ConfigMap contem apenas configuracao nao sensivel:

- `ASPNETCORE_ENVIRONMENT`
- `ASPNETCORE_HTTP_PORTS=8080`
- `JwtSettings__Issuer=car-repair-auth`
- `JwtSettings__Audience=car-repair-shop`
- `JwtSettings__ExpirationMinutes=60`
- `Database__RunMigrationsOnStartup=false`
- `AuthLambda__BaseUrl`
- `AuthLambda__TokenPath=/auth/token`
- `SmtpSettings__Host`
- `SmtpSettings__Port`
- `SmtpSettings__UseSsl`
- `SmtpSettings__FromEmail`
- `SmtpSettings__FromName`
- configuracoes publicas de SMTP/AppSettings conforme necessario

`AuthLambda__BaseUrl` e configurado por ambiente nos overlays. Enquanto a URL final nao existir, use o placeholder do ambiente. O valor final deve vir do deploy `car-repair-auth-lambda` e da arquitetura de gateway que publicar esse endpoint.

Nao coloque senha, connection string, chave JWT ou credenciais SMTP no ConfigMap.

## Migrations

A API nao executa `Database.Migrate()` automaticamente no startup cloud.

Foi criado um modo explicito no mesmo binario:

```bash
dotnet CarRepairShop.API.dll --migrate
```

O Kubernetes Job `migration-job.yaml` usa a mesma imagem da aplicacao e executa esse comando antes do rollout. Isso evita multiplas replicas tentando aplicar migrations simultaneamente.

Ordem obrigatoria do deployment cloud:

```text
prerequisites
 ↓
ExternalSecret ready
 ↓
migration Job
 ↓
Deployment
 ↓
rollout
```

O workflow aplica `k8s/overlays/<environment>/prerequisites`, aguarda o `ExternalSecret` e o Kubernetes Secret `car-repair-app-secrets`, recria e aguarda o `Job` de migration, e somente depois aplica `k8s/overlays/<environment>/workload`. Assim o `Deployment`, o `Service` e o `HPA` nao sao criados ou atualizados antes da migration concluir com sucesso.

Para desenvolvimento local via Docker Compose, `Database__RunMigrationsOnStartup=true` continua disponivel para simplificar o fluxo local.

## Docker

O Dockerfile usa:

- .NET 8 SDK para build
- .NET 8 ASP.NET runtime para execucao
- multi-stage build
- usuario non-root
- porta `8080`

A imagem Docker nao declara `HEALTHCHECK`. No EKS, a saude da aplicacao e verificada pelos probes Kubernetes (`readinessProbe` e `livenessProbe`) em `/health`, sem instalar `curl`, `wget` ou outros utilitarios apenas para health check.

Build local:

```bash
docker build -t car-repair-app:local .
```

## ECR e tags

O ECR e criado pelo `car-repair-k8s-infra`.

Use tags imutaveis baseadas no commit:

```text
<git-sha>
```

Nao use `latest` em producao.

Imagem final:

```text
<ECR_REPOSITORY_URL>:<git-sha>
```

No deploy, atualize a imagem com Kustomize:

```bash
cd k8s/overlays/<environment>/migration
kustomize edit set image car-repair-app="$ECR_REPOSITORY_URL:$IMAGE_TAG"

cd ../workload
kustomize edit set image car-repair-app="$ECR_REPOSITORY_URL:$IMAGE_TAG"
```

## CI/CD

Ambientes no repositorio:

- `academy-dev`
- `dev`
- `hml`
- `prod`

Fluxo de deploy automatizado em `.github/workflows/cd.yml`:

- push na branch `hml`: deploy em `hml`
- `workflow_dispatch`: deploy manual para `hml` ou `prod`

O workflow mantem:

- `dotnet restore`
- `dotnet build`
- unit tests com coverage
- integration tests com PostgreSQL via Testcontainers
- renderizacao dos manifests Kustomize

O fluxo cloud preparado e condicionado por variaveis GitHub:

1. autenticacao AWS via GitHub OIDC
2. `docker build`
3. push para ECR
4. `aws eks update-kubeconfig`
5. aplicar prerequisites
6. aguardar `ExternalSecret` ready e o Kubernetes Secret `car-repair-app-secrets`
7. recriar e aguardar o migration Job
8. aplicar Deployment, Service, HPA e gateway
9. aguardar `kubectl rollout status`

Variaveis necessarias:

- `AWS_REGION`
- `ENVIRONMENT`
- `ECR_REPOSITORY_URL`
- `EKS_CLUSTER_NAME`

Secret GitHub necessario:

- `AWS_ROLE_TO_ASSUME`

Nao use AWS access key/secret fixas.

## Desenvolvimento local

Pre-requisitos:

- Docker
- Docker Compose
- portas `8080` (API) e `5432` (PostgreSQL) livres

Docker Compose usa PostgreSQL local:

```bash
export POSTGRES_PASSWORD='local-postgres-password'
export JWT_SECRET_KEY='local-jwt-secret-with-at-least-32-characters'
export AuthLambda__BaseUrl='http://localhost:3000'
docker compose up --build
```

> Use um endpoint local/mock para `AuthLambda__BaseUrl` se quiser validar o fluxo de login ponta a ponta.

A API fica em:

```text
http://localhost:8080
```

Health:

```bash
curl http://localhost:8080/health
```

## Documentacao da API

Em ambiente `Development`, o Swagger UI fica disponivel em:

```text
http://localhost:8080/swagger
```

No momento, este repositorio nao versiona colecao Postman.

## Testes

```bash
dotnet restore
dotnet build
dotnet test
```

Os testes de integracao usam PostgreSQL via Testcontainers.

## Validacao Kubernetes

Renderizacao completa de um ambiente:

```bash
kubectl kustomize k8s/overlays/<environment>
kubectl kustomize k8s/overlays/<environment>/prerequisites
kubectl kustomize k8s/overlays/<environment>/migration
kubectl kustomize k8s/overlays/<environment>/workload
kubectl kustomize k8s/overlays/<environment>/gateway
```

Ambientes existentes no repositorio:

```text
academy-dev, dev, hml, prod
```

## Fora do escopo atual

- Redis
- OAuth/OIDC
- mTLS
- Service Mesh
- Keycloak/Cognito
- RDS Proxy
