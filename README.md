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
  - Controllers `*Ui` — endpoints para renderização de views (interface web)
- **Middleware** — tratamento global de exceções
- **Program.cs** — configuração de DI, autenticação JWT, Swagger e pipeline HTTP

---

## Setup com Docker (recomendado)

> **Pré-requisito:** [Docker Desktop](https://www.docker.com/get-started) (ou Docker Engine + Compose plugin) instalado e em execução.

O ambiente Docker sobe dois containers:

| Container | Imagem | Porta |
|-----------|--------|-------|
| `db` | `mcr.microsoft.com/mssql/server:2022-latest` | `1433` |
| `api` | Build local (Dockerfile multi-stage) | `8080` |

O container `api` só inicia após o `db` passar no healthcheck, garantindo que o SQL Server esteja pronto para aceitar conexões antes das migrations serem aplicadas.

### Passo a passo

**1. Clonar o repositório**

```bash
git clone https://github.com/Mavidev-Solucoes/car-repair-shop.git
cd car-repair-shop
```

**2. Criar o arquivo `.env`**

```bash
cp .env.example .env
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
cd src/CarRepairShop.API

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost;Database=CarRepairShopDb;User Id=sa;Password=SuaSenha!;TrustServerCertificate=True;"
dotnet user-secrets set "JwtSettings:SecretKey" "SuaChaveSecretaComPeloMenos32Caracteres!"
```

### 2. Aplicar migrations

```bash
# Requer dotnet-ef instalado globalmente:
# dotnet tool install --global dotnet-ef

dotnet ef database update \
  --project src/CarRepairShop.Repository \
  --startup-project src/CarRepairShop.API
```

### 3. Executar a API

```bash
dotnet run --project src/CarRepairShop.API
```

Acesse o Swagger em: `https://localhost:<porta>/swagger`

### Criar uma nova migration (desenvolvimento)

```bash
dotnet ef migrations add NomeDaMigration \
  --project src/CarRepairShop.Repository \
  --startup-project src/CarRepairShop.API
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
