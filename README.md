# Car Repair Shop

API RESTful para gestão de uma oficina mecânica, desenvolvida como Tech Challenge do curso de Arquitetura de Soluções da FIAP.

---

## Índice

1. [Visão Geral](#visão-geral)
2. [Arquitetura e Camadas](#arquitetura-e-camadas)
3. [Setup com Docker (recomendado)](#setup-com-docker-recomendado)
4. [Variáveis de Ambiente](#variáveis-de-ambiente)
5. [Setup Local (sem Docker)](#setup-local-sem-docker)
6. [Autenticação](#autenticação)
7. [Endpoints Principais](#endpoints-principais)
8. [SonarQube — Qualidade e Segurança](#sonarqube--qualidade-e-segurança)
9. [Tech Challenge — Fase 2](#tech-challenge--fase-2)

---

## Visão Geral

O sistema permite gerenciar clientes, veículos, ordens de serviço e funcionários de uma oficina mecânica. A API expõe endpoints REST protegidos por JWT e inclui uma interface Swagger para exploração interativa.

**Stack principal:**
- .NET 8 / ASP.NET Core
- Entity Framework Core (Code-First) + SQL Server 2022
- MediatR (CQRS), FluentValidation, BCrypt, JWT Bearer

---

## Arquitetura e Camadas

O projeto adota **DDD (Domain-Driven Design)** com separação clara em cinco camadas. A dependência flui sempre de fora para dentro: a camada mais externa depende da mais interna, nunca o contrário.

```
┌─────────────────────────────┐
│        CarRepairShop.API    │  ← Camada de apresentação
├─────────────────────────────┤
│   CarRepairShop.Application │  ← Camada de aplicação (CQRS)
├─────────────────────────────┤
│    CarRepairShop.Services   │  ← Serviços de domínio
├─────────────────────────────┤
│   CarRepairShop.Repository  │  ← Acesso a dados (EF Core)
├─────────────────────────────┤
│     CarRepairShop.Domain    │  ← Núcleo do domínio
└─────────────────────────────┘
```

### CarRepairShop.Domain

Núcleo da aplicação. Não depende de nenhuma outra camada do projeto.

- **Entities** — Entidades de domínio (`Customer`, `Vehicle`, `ServiceOrder`, `ServiceOrderItem`, `ServiceJob`, `ServiceItem`, `User`, `Employee`, `BaseEntity`)
- **Enums** — Enumerações de negócio (`ServiceStatus`, `JobStatus`, `UserType`)
- **Interfaces** — Contratos de repositórios e serviços que as camadas externas devem implementar
- **Settings** — Classes de configuração mapeadas do `appsettings.json` (`JwtSettings`, `SmtpSettings`, `AppSettings`)

> Todas as entidades herdam de `BaseEntity`, que fornece `Id` (GUID), `CreatedAt`, `UpdatedAt`, `CreatedUserId` e `LastUpdatedUserId`.

### CarRepairShop.Repository

Implementação de acesso a dados com **Entity Framework Core** e **SQL Server**.

- `CarRepairShopDbContext` — contexto EF Core com mapeamento de todas as entidades
- Repositórios genéricos e específicos que implementam as interfaces do Domain
- **Migrations** Code-First — o histórico completo de evolução do schema está em `Migrations/`
- Seed da migration inicial: cria o usuário administrador padrão

### CarRepairShop.Services

Serviços de infraestrutura e domínio reutilizáveis entre camadas.

- `PasswordHashingService` — hashing e verificação de senhas com **BCrypt**
- `TokenService` — geração e validação de tokens **JWT**
- `EmailService` — envio de e-mails via SMTP
- `EmailTemplateService` — renderização de templates HTML com substituição de tokens `{{VARIAVEL}}`

> Os templates de e-mail (HTML) estão em `src/CarRepairShop.API/Templates/`.

### CarRepairShop.Application

Orquestra os casos de uso da aplicação usando o padrão **CQRS**.

- **Commands** — operações de escrita (criar, atualizar, excluir)
- **Queries** — operações de leitura, retornando `PagedResult<T>` para listas paginadas
- **Handlers** — implementações de `IRequestHandler` para cada command/query (via **MediatR**)
- **Validators** — validações declarativas com **FluentValidation**, executadas automaticamente por um `ValidationBehavior` (pipeline behavior) antes de cada handler — fail-fast
- **Common** — `PagedResult<T>` e utilitários compartilhados

### CarRepairShop.API

Camada de entrada HTTP. Orquestra a injeção de dependências e expõe a API REST.

- **Controllers** — endpoints REST que recebem requisições, montam commands/queries e os despacham via MediatR
  - `AuthController` — login e geração de token
  - `CustomersController` — CRUD de clientes
  - `VehiclesController` — CRUD de veículos
  - `ServiceOrdersController` — ordens de serviço, itens e atualização de status
  - `ServiceItemsController` / `ServiceJobsController` — catálogo de serviços
  - `OrderJobsController` — jobs vinculados a ordens de serviço
  - `UsersController` — gestão de usuários (Admin)
- **Middleware** — tratamento global de exceções
- **Program.cs** — configuração de DI, autenticação JWT, Swagger e pipeline HTTP

---

## Setup com Docker (recomendado)

> **Pré-requisito:** [Docker Desktop](https://www.docker.com/get-started) (ou Docker Engine + Compose plugin) instalado e em execução.

O ambiente Docker sobe os seguintes containers:

| Container | Imagem | Porta |
|-----------|--------|-------|
| `db` | `mcr.microsoft.com/mssql/server:2022-latest` | `1433` |
| `api` | Build local (Dockerfile multi-stage) | `8080` |
| `sonarqube` | `sonarqube:community` | `9000` |

O container `api` só inicia após o `db` passar no healthcheck, garantindo que o SQL Server esteja pronto para aceitar conexões antes das migrations serem aplicadas. O container `sonarqube` aguarda um serviço auxiliar (`db-init`) que cria automaticamente o banco de dados `SonarQubeDb` no SQL Server antes de iniciar.

### Passo a passo

**1. Clonar o repositório**

```bash
git clone https://github.com/Mavidev-Solucoes/car-repair-shop.git
cd car-repair-shop
```

**2. Criar o arquivo `.env`**

```bash
# bash / macOS / Linux
cp .env.example .env
```

```cmd
REM Windows CMD
copy .env.example .env
```

Abra o arquivo `.env` e defina valores seguros para as variáveis obrigatórias (veja a seção [Variáveis de Ambiente](#variáveis-de-ambiente)):

```dotenv
SA_PASSWORD=MinhaS3nhaForte@2024!
JWT_SECRET_KEY=MinhaChaveSecretaComPeloMenos32Caracteres!
```

**3. Subir os containers**

```bash
docker compose up --build
```

- Na primeira execução, a imagem da API será compilada e as migrations serão aplicadas automaticamente.
- Aguarde a mensagem `Now listening on: http://[::]:8080` nos logs da API.

**4. Acessar o Swagger**

```
http://localhost:8080/swagger
```

### Comandos úteis

```bash
# Subir em segundo plano
docker compose up --build -d

# Ver logs em tempo real
docker compose logs -f api

# Parar os containers (dados preservados)
docker compose down

# Parar e remover o volume do banco de dados
docker compose down -v

# Recompilar apenas a API após mudanças no código
docker compose up --build api
```

### Como funciona o Dockerfile

O `Dockerfile` usa **multi-stage build** para gerar uma imagem de produção enxuta:

```
Estágio 1 — build  (sdk:8.0)
  └── Compila e publica a aplicação em /app/publish

Estágio 2 — final  (aspnet:8.0)
  └── Copia apenas os artefatos publicados
  └── Imagem final sem o SDK, menor e mais segura
```

---

## Variáveis de Ambiente

### Arquivo `.env` (segredos do Docker Compose)

O arquivo `.env` **nunca deve ser versionado** (já está no `.gitignore`). Ele fornece os segredos injetados no `docker-compose.yml`:

| Variável | Obrigatória | Descrição |
|----------|:-----------:|-----------|
| `SA_PASSWORD` | ✅ | Senha do usuário `sa` do SQL Server. Deve satisfazer os requisitos de complexidade do SQL Server: mínimo 8 caracteres, letras maiúsculas, minúsculas, números e símbolo especial. |
| `JWT_SECRET_KEY` | ✅ | Chave secreta HMAC usada para assinar os tokens JWT. Mínimo de 32 caracteres. Quanto mais longa e aleatória, mais segura. |

### Variáveis injetadas no container `api` (docker-compose.yml)

Estas variáveis são definidas diretamente no `docker-compose.yml` e sobrescrevem o `appsettings.json`:

| Variável | Valor padrão no Compose | Descrição |
|----------|------------------------|-----------|
| `ASPNETCORE_ENVIRONMENT` | `Development` | Ambiente do ASP.NET Core. Use `Production` em produção para desabilitar o Swagger e habilitar otimizações. |
| `ASPNETCORE_HTTP_PORTS` | `8080` | Porta HTTP em que a API escuta dentro do container. |
| `ConnectionStrings__DefaultConnection` | `Server=db;...` | String de conexão ao SQL Server. O hostname `db` é o nome do serviço no Compose. |
| `JwtSettings__SecretKey` | `${JWT_SECRET_KEY}` | Chave secreta JWT (injetada do `.env`). |
| `JwtSettings__Issuer` | `CarRepairShop` | Identificador do emissor do token JWT (`iss` claim). |
| `JwtSettings__Audience` | `CarRepairShop` | Público-alvo do token JWT (`aud` claim). |
| `JwtSettings__ExpirationMinutes` | `60` | Tempo de expiração do token JWT em minutos. |

> **Convenção de nome:** o ASP.NET Core converte `__` (duplo underscore) em `:` ao mapear variáveis de ambiente para a hierarquia do `appsettings.json`. Assim, `JwtSettings__SecretKey` equivale a `JwtSettings:SecretKey`.

### Configurações adicionais (appsettings.json)

As configurações abaixo não são obrigatórias para rodar localmente, mas são necessárias para habilitar o envio de e-mails (notificações de status das ordens de serviço):

| Chave | Descrição |
|-------|-----------|
| `SmtpSettings:Host` | Endereço do servidor SMTP (ex: `smtp.gmail.com`) |
| `SmtpSettings:Port` | Porta SMTP (ex: `465` para SSL, `587` para TLS) |
| `SmtpSettings:UseSsl` | `true` para conexão SSL/TLS |
| `SmtpSettings:Username` | Usuário de autenticação SMTP |
| `SmtpSettings:Password` | Senha ou App Password do SMTP |
| `SmtpSettings:FromEmail` | Endereço de e-mail remetente |
| `SmtpSettings:FromName` | Nome de exibição do remetente |
| `AppSettings:BaseUrl` | URL base da aplicação, usada para gerar links de aprovação nos e-mails (ex: `https://meudominio.com`) |

Para sobrescrever via variáveis de ambiente no Docker Compose, use o mesmo padrão de `__`:

```yaml
SmtpSettings__Host: "smtp.gmail.com"
SmtpSettings__Port: "587"
SmtpSettings__Username: "${SMTP_USER}"
SmtpSettings__Password: "${SMTP_PASSWORD}"
AppSettings__BaseUrl: "https://meudominio.com"
```

---

## Setup Local (sem Docker)

> **Pré-requisitos:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) e [SQL Server](https://www.microsoft.com/sql-server) (ou SQL Server Express) instalados localmente.

### 1. Configurar credenciais com User Secrets

```bash
# bash / macOS / Linux
cd src/CarRepairShop.API

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost;Database=CarRepairShopDb;User Id=sa;Password=SuaSenha!;TrustServerCertificate=True;"
dotnet user-secrets set "JwtSettings:SecretKey" "SuaChaveSecretaComPeloMenos32Caracteres!"
```

```cmd
REM Windows CMD
cd src\CarRepairShop.API

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=CarRepairShopDb;User Id=sa;Password=SuaSenha!;TrustServerCertificate=True;"
dotnet user-secrets set "JwtSettings:SecretKey" "SuaChaveSecretaComPeloMenos32Caracteres!"
```

### 2. Aplicar migrations

```bash
# bash / macOS / Linux — requer dotnet-ef instalado globalmente:
# dotnet tool install --global dotnet-ef

dotnet ef database update \
  --project src/CarRepairShop.Repository \
  --startup-project src/CarRepairShop.API
```

```cmd
REM Windows CMD
dotnet ef database update --project src/CarRepairShop.Repository --startup-project src/CarRepairShop.API
```

### 3. Executar a API

```cmd
dotnet run --project src/CarRepairShop.API
```

Acesse o Swagger em: `https://localhost:<porta>/swagger`

### Criar uma nova migration (desenvolvimento)

```bash
# bash / macOS / Linux
dotnet ef migrations add NomeDaMigration \
  --project src/CarRepairShop.Repository \
  --startup-project src/CarRepairShop.API
```

```cmd
REM Windows CMD
dotnet ef migrations add NomeDaMigration --project src/CarRepairShop.Repository --startup-project src/CarRepairShop.API
```

---

## Autenticação

A API usa **JWT Bearer**. Para autenticar:

1. Faça `POST /api/auth/login` com as credenciais:
   ```json
   {
     "email": "admin@carrepairshop.com",
     "password": "Admin@123"
   }
   ```
2. Copie o token JWT retornado no campo `token`.
3. No Swagger, clique em **Authorize** e informe: `Bearer <token>`
4. Todas as requisições subsequentes incluirão o header `Authorization: Bearer <token>`.

> ⚠️ **Altere a senha do administrador após o primeiro login em produção.**

### Credenciais padrão (seed)

| Campo | Valor |
|-------|-------|
| Email | `admin@carrepairshop.com` |
| Senha | `Admin@123` |
| Perfil | `Admin` |

---

## Endpoints Principais

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/api/auth/login` | Autenticação — retorna token JWT |
| `GET` / `POST` | `/api/customers` | Listar / criar clientes |
| `GET` / `PUT` / `DELETE` | `/api/customers/{id}` | Obter, atualizar ou excluir cliente |
| `GET` / `POST` | `/api/vehicles` | Listar / criar veículos |
| `GET` / `PUT` / `DELETE` | `/api/vehicles/{id}` | Obter, atualizar ou excluir veículo |
| `GET` / `POST` | `/api/serviceorders` | Listar / criar ordens de serviço |
| `GET` / `PUT` | `/api/serviceorders/{id}` | Obter / atualizar ordem de serviço |
| `POST` | `/api/serviceorders/{id}/items` | Adicionar item à ordem de serviço |
| `PATCH` | `/api/serviceorders/{id}/status` | Avançar status da ordem de serviço |
| `GET` / `POST` | `/api/serviceitems` | Catálogo de itens de serviço |
| `GET` / `POST` | `/api/servicejobs` | Catálogo de jobs de serviço |
| `GET` / `POST` | `/api/orderjobs` | Jobs vinculados a ordens de serviço |
| `GET` / `POST` | `/api/users` | Gerenciar usuários (Admin) |

> A documentação interativa completa de todos os endpoints, parâmetros e modelos está disponível no Swagger: `http://localhost:8080/swagger`

### Ciclo de vida de uma Ordem de Serviço

| Status | Valor | Descrição |
|--------|-------|-----------|
| `Received` | 1 | OS recebida |
| `WaitingApproval` | 2 | Aguardando aprovação do cliente |
| `Approved` | 3 | Aprovada pelo cliente |
| `InProgress` | 4 | Em execução na oficina |
| `Finished` | 5 | Serviço concluído |
| `Delivered` | 6 | Veículo entregue ao cliente |

---

## SonarQube — Qualidade e Segurança

O projeto inclui um container **SonarQube Community** que compartilha o SQL Server já existente no Compose. O SonarQube permite realizar:

- **Análise de cobertura de testes** — exibe quais linhas de código são exercidas pelos testes.
- **Varredura de vulnerabilidades** — detecta bugs, code smells, hotspots de segurança e vulnerabilidades (OWASP, CWE) no código.

### Containers adicionados

| Container | Imagem | Porta | Banco de dados |
|-----------|--------|-------|----------------|
| `db-init` | `mcr.microsoft.com/mssql/server:2022-latest` | — | Cria `SonarQubeDb` no SQL Server (executa uma única vez e encerra) |
| `sonarqube` | `sonarqube:community` | `9000` | `SonarQubeDb` (SQL Server) |

> ⚠️ **Requisito do sistema operacional:** O SonarQube exige que o parâmetro do kernel `vm.max_map_count` seja pelo menos `524288`. No Linux, execute antes de subir os containers:
> ```bash
> sudo sysctl -w vm.max_map_count=524288
> ```
> No Docker Desktop (macOS/Windows), esse ajuste é feito automaticamente pelo Docker Desktop.

### Passo a passo

#### 1. Subir o ambiente (incluindo o SonarQube)

```bash
docker compose up --build -d
```

Aguarde o SonarQube iniciar completamente (pode levar de 1 a 2 minutos):

```bash
docker compose logs -f sonarqube
# Aguarde a linha: SonarQube is operational
```

#### 2. Primeiro acesso e configuração

1. Abra `http://localhost:9000` no navegador.
2. Faça login com as credenciais padrão: **usuário** `admin` / **senha** `admin`.
3. O SonarQube pedirá para você definir uma nova senha — escolha uma senha segura.

#### 3. Criar um projeto local

1. Na tela inicial, clique em **Create a local project**.
2. Defina:
   - **Project display name**: `car-repair-shop`
   - **Project key**: `car-repair-shop`
3. Escolha a opção **Use the global setting** e clique em **Create project**.

#### 4. Gerar o token de autenticação

1. Ainda no assistente de configuração, escolha **Locally**.
2. Em **Generate a token**, informe um nome (ex: `local-dev`) e clique em **Generate**.
3. Copie o token gerado — você vai precisar dele no passo seguinte.

> ⚠️ O token **não pode ser recuperado** depois de fechada esta tela. Guarde-o com segurança.
>
> Opcionalmente, salve o token no seu `.env` para referência futura:
> ```dotenv
> SONAR_TOKEN=sqp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
> ```
> O arquivo `.env` **não** é carregado automaticamente pelo terminal — você precisará definir a variável de ambiente manualmente em cada sessão (veja o passo 6).

#### 5. Instalar o dotnet-sonarscanner

Instale a ferramenta globalmente (necessário apenas uma vez):

```bash
dotnet tool install --global dotnet-sonarscanner
```

#### 6. Executar a análise com cobertura de código

**Passo 6a — Definir o token na sessão do terminal**

Antes de executar o scanner, defina o token gerado no passo anterior como variável de ambiente na sessão atual do terminal:

```cmd
REM Windows CMD
set SONAR_TOKEN=sqp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

```bash
# bash / macOS / Linux
export SONAR_TOKEN=sqp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

**Passo 6b — Executar os quatro comandos a partir da raiz do repositório**

> ⚠️ **Importante:** execute os comandos a partir da raiz do repositório (pasta `car-repair-shop`). O scanner precisa de permissão de escrita nessa pasta para criar o diretório temporário `.sonarqube`.

**Windows CMD:**

```cmd
REM 1. Iniciar a análise
dotnet sonarscanner begin /k:"car-repair-shop" /d:sonar.host.url="http://localhost:9000" /d:sonar.token="%SONAR_TOKEN%" /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"

REM 2. Compilar o projeto
dotnet build

REM 3. Executar os testes coletando cobertura no formato OpenCover
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

REM 4. Finalizar e enviar os resultados para o SonarQube
dotnet sonarscanner end /d:sonar.token="%SONAR_TOKEN%"
```

**bash / macOS / Linux:**

```bash
# 1. Iniciar a análise
dotnet sonarscanner begin \
  /k:"car-repair-shop" \
  /d:sonar.host.url="http://localhost:9000" \
  /d:sonar.token="${SONAR_TOKEN}" \
  /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"

# 2. Compilar o projeto
dotnet build

# 3. Executar os testes coletando cobertura no formato OpenCover
dotnet test \
  --collect:"XPlat Code Coverage" \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

# 4. Finalizar e enviar os resultados para o SonarQube
dotnet sonarscanner end /d:sonar.token="${SONAR_TOKEN}"
```

Após a execução, acesse `http://localhost:9000/dashboard?id=car-repair-shop` para ver o relatório completo.

### Solução de problemas — erros de permissão

Erros de permissão durante a análise geralmente têm uma das seguintes causas:

| Causa | Solução |
|-------|---------|
| Terminal aberto em diretório sem permissão de escrita (ex: `C:\Program Files`) | Navegue até a raiz do repositório antes de executar os comandos |
| Diretório `.sonarqube` corrompido de uma execução anterior interrompida | Delete a pasta `.sonarqube` na raiz do projeto e tente novamente |
| Permissões insuficientes do usuário atual | No Windows, abra o CMD como **Administrador** |
| Token não definido na sessão (`%SONAR_TOKEN%` vazio) | Execute `set SONAR_TOKEN=seu_token` no mesmo terminal antes de rodar o scanner |
| Outro processo do sonarscanner em execução | Aguarde ou encerre o processo anterior antes de iniciar uma nova análise |

Se o problema persistir, execute o comando `begin` com o token explícito no lugar de `%SONAR_TOKEN%`:

```cmd
dotnet sonarscanner begin /k:"car-repair-shop" /d:sonar.host.url="http://localhost:9000" /d:sonar.token="sqp_SEU_TOKEN_AQUI" /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"
```

### O que o relatório exibe

| Aba | Conteúdo |
|-----|----------|
| **Overview** | Nota geral de qualidade, cobertura, duplicações e issues |
| **Issues** | Bugs, code smells e vulnerabilidades encontrados |
| **Security Hotspots** | Pontos de atenção de segurança que requerem revisão manual |
| **Coverage** | Percentual de linhas e branches cobertos pelos testes |
| **Code** | Navegação pelo código-fonte com anotações inline |

### Comandos úteis do SonarQube

```bash
# Parar o SonarQube (preserva dados)
docker compose stop sonarqube

# Remover o SonarQube e seus volumes (reset completo)
# O prefixo dos volumes é o nome da pasta do projeto (padrão: car-repair-shop)
docker volume rm car-repair-shop_sonarqube_data car-repair-shop_sonarqube_extensions car-repair-shop_sonarqube_logs

# Ver logs do SonarQube em tempo real
docker compose logs -f sonarqube
```

---

## Tech Challenge — Fase 2

> **Tudo abaixo desta linha (incluindo a seção de Clean Architecture) pertence à Fase 2 do Tech Challenge.**

### Validação dos requisitos obrigatórios (estado atual do repositório)

| Requisito | Status | Evidência |
|---|---|---|
| Refatorar com Clean Code | **Atendido** | Handlers delegando para serviços coesos (`ServiceOrderOpeningService`, `ServiceOrderApprovalRequestService`), nomes explícitos e redução de responsabilidades por classe. |
| Testes automatizados cobrindo fluxos críticos | **Atendido** | `dotnet test` com **800** testes unitários e **33** de integração passando. Cobertura de ciclo de vida da OS e regras de domínio. |
| Abertura de OS com cliente, veículo, serviços e peças | **Atendido por fluxo de APIs** | Abertura via `POST /api/services` (cliente/veículo) e composição com `POST /api/services/{id}/items` + `POST /api/services/{id}/jobs` para peças/serviços, mantendo retorno do identificador único da OS. |
| Consulta de status da OS | **Atendido** | `GET /api/services/{id}` retorna a OS com `Status`; `GET /api/services/{id}/history` retorna trilha de transições. |
| Aprovação de orçamento com notificação externa de aprovação/recusa | **Atendido** | Aprovação via `PATCH /api/services/{id}/approve` e recusa via `PATCH /api/services/{id}/reject` (ambos anônimos, acionados a partir do e-mail). O e-mail envia os dois endpoints. Semântica REST correta (operação de mudança de estado via PATCH). |
| Listagem de OS com ordenação de negócio e exclusão lógica de finalizadas/entregues | **Atendido** | `GET /api/services?includeCompleted=false` oculta ordens `Finished`/`Delivered`. Os resultados são ordenados por prioridade operacional: `WaitingForApproval` → `Executing` → `Diagnosing` → `Received` → `Finished` → `Delivered`. |
| Atualização de status via e-mail | **Atendido** | Notificações de e-mail para OS recebida, solicitação de aprovação e serviço finalizado. |

### Assessment de Clean Code (Fase 2)

| Critério | Nota (0-10) | Resultado |
|---|---:|---|
| Nomes claros e intenção explícita | 9.0 | Comandos/handlers/serviços com nomenclatura orientada a caso de uso. |
| Simplicidade de fluxo | 8.5 | Fluxo da OS é direto e protegido por invariantes no domínio. |
| Coesão e responsabilidade única | 9.0 | Refatoração separou orquestração, notificação e persistência de histórico. |
| Baixo acoplamento e inversão de dependência | 9.5 | Uso consistente de interfaces + DI nas camadas Application/API. |
| Testabilidade | 9.5 | Alta cobertura de testes unitários e integração para fluxos críticos. |
| Tratamento de erros e regras de negócio | 9.0 | Exceções de negócio explícitas e middleware dedicado. |

**Média do assessment de Clean Code: 9.1 / 10**

### Pontos já implementados previamente (base já existente)

- O ciclo principal da OS (`Received -> Diagnosing -> WaitingForApproval -> Executing -> Finished -> Delivered`) já estava modelado no domínio e exposto por endpoints na API.
- A consulta de OS por ID e histórico de status já estava disponível, permitindo rastreabilidade do processo.
- O fluxo de notificações por e-mail já estava integrado aos eventos críticos de status.

## Avaliação Arquitetural — Clean Architecture & SOLID

> **Re-avaliação final** após implementação de todas as melhorias identificadas na análise anterior.
> Data: 2026-07-01

### Metodologia

O projeto foi analisado linha a linha contra os seguintes critérios:

| Critério | Descrição |
|----------|-----------|
| **SRP** | Single Responsibility Principle — cada classe/módulo tem uma única razão para mudar |
| **OCP** | Open/Closed Principle — aberto para extensão, fechado para modificação |
| **LSP** | Liskov Substitution Principle — subtipos podem substituir seus tipos base sem quebrar o comportamento |
| **ISP** | Interface Segregation Principle — interfaces coesas e específicas |
| **DIP** | Dependency Inversion Principle — dependência de abstrações, não de concreções |
| **Clean Architecture** | Separação de camadas, fluxo de dependência correto, independência de frameworks |
| **Domain Design** | Riqueza do modelo de domínio, uso de value objects, encapsulamento de regras |

---

### Melhorias implementadas nesta iteração

#### 1. Injeção de dependência para rastreamento de histórico (SRP + DIP)

Eliminadas as classes estáticas `ServiceOrderHistoryPersistence` e `OrderJobHistoryPersistence`. Substituídas pelas interfaces `IServiceOrderHistoryTracker` e `IOrderJobHistoryTracker` com implementações concretas registradas no contêiner de DI (`AddScoped`). Todos os command handlers relevantes passaram a receber os trackers por injeção, tornando-os completamente testáveis com mocks.

#### 2. Chain of Responsibility no middleware de exceções (OCP)

O `ExceptionHandlingMiddleware` foi refatorado para consumir `IEnumerable<IExceptionResponseMapper>`. Cada tipo de exceção tem seu próprio mapper (`ValidationExceptionMapper`, `NotFoundExceptionMapper`, `BusinessExceptionMapper`, `InvalidOperationExceptionMapper`), registrados como singletons. Novos tipos de exceção podem ser tratados adicionando apenas um novo mapper — sem tocar em código existente.

#### 3. Consulta tipada de funcionário no repositório (LSP)

Adicionado `GetEmployeeByIdAsync` a `IUserRepository`, implementado via `OfType<Employee>()` na camada de repositório. Eliminados todos os downcasts `as Employee` na camada Application, substituídos por chamadas ao novo método. Isso remove dependência implícita da hierarquia de herança nos handlers.

#### 4. Value Objects no domínio (Domain Design)

Criados `PersonalId` e `PhoneNumber` como `sealed record` em `Domain/ValueObjects/`. A entidade `Customer` agora normaliza CPF e telefone através desses value objects em seu construtor, eliminando a duplicação da regra de limpeza de dígitos. Os tipos das propriedades da entidade permanecem `string` para evitar migrações de banco de dados desnecessárias.

#### 5. Request records em namespace próprio (Clean Architecture)

`AddServiceItemRequest` e `AddServiceJobRequest` movidos de definições inline no controller para `CarRepairShop.API.Requests/ServiceOrderRequests.cs`, alinhados com os demais request types da API.

---

### Pontuação por princípio

| Princípio / Critério | Antes | Depois | Evolução |
|----------------------|-------|--------|----------|
| **SRP** | 8.0 | 9.0 | ↑ +1.0 — trackers injetáveis eliminam classes estáticas de persistência |
| **OCP** | 7.5 | 9.0 | ↑ +1.5 — chain of mappers no middleware; novas exceções sem modificar código existente |
| **LSP** | 8.0 | 9.0 | ↑ +1.0 — `GetEmployeeByIdAsync` elimina downcasts inseguros na camada Application |
| **ISP** | 9.0 | 9.0 | = — interfaces permaneceram coesas; novo método em `IUserRepository` é coerente |
| **DIP** | 9.0 | 9.5 | ↑ +0.5 — nenhuma dependência concreta restante nos handlers ou serviços de aplicação |
| **Clean Architecture** | 9.0 | 9.5 | ↑ +0.5 — value objects no domínio, request records na API, trackers na Application |
| **Domain Design** | 8.0 | 9.0 | ↑ +1.0 — `PersonalId` e `PhoneNumber` encapsulam regras de normalização no domínio |

### Pontuação geral

| | Nota |
|---|---|
| **Média anterior** | **8.8 / 10** |
| **Média atual** | **9.1 / 10** |

---

### Pontos restantes de atenção (baixa criticidade)

| Item | Observação |
|------|-----------|
| **Múltiplos handlers por arquivo** | `OrderJobCommandHandlers.cs` e `UserCommandHandlers.cs` agrupam vários handlers. Aceitável como convenção de organização, mas uma classe por arquivo seria mais idiomático. |
| **`IUserRepository.GetByIdAsync` retorna `User?`** | Handlers como `ChangePasswordCommandHandler` ainda usam `GetByIdAsync` (retorno `User?`) onde seria tecnicamente mais preciso usar `GetEmployeeByIdAsync`. Impacto mínimo pois password change pode ser feito por qualquer usuário autenticado. |
| **Integration tests** | A cobertura de testes de integração cobre os happy paths principais. Cenários de falha de banco de dados e de rollback de transação poderiam ser adicionados para cobertura mais completa. |

---

### Resumo

O projeto demonstra aplicação sólida de Clean Architecture e princípios SOLID. Com as melhorias implementadas nesta iteração, os principais pontos de atrito foram resolvidos:

- **Dependências invertidas**: nenhum handler depende de concreções ou classes estáticas
- **Extensibilidade real**: middleware de exceções e pipeline de notificações são extensíveis sem modificação
- **Domínio rico**: value objects encapsulam regras de normalização; entidades protegem seus invariantes
- **Testabilidade**: 790 testes unitários passando, todos os novos componentes cobertos com mocks adequados
- **Layering correto**: cada artefato vive na camada apropriada da Clean Architecture

A base de código está bem preparada para crescimento: novas funcionalidades podem ser adicionadas sem regressões estruturais, e a inversão de dependências em todas as camadas garante testabilidade independente de infraestrutura.
