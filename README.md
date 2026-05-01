# car-repair-shop
Esse projeto faz parte do Tech Challenge do curso de Arquitetura de Soluções da FIAP

## Executar com Docker (recomendado)

> Pré-requisito: [Docker](https://www.docker.com/get-started) instalado.

```bash
# Clonar o repositório
git clone https://github.com/Mavidev-Solucoes/car-repair-shop.git
cd car-repair-shop

# Criar o arquivo de variáveis de ambiente a partir do template
cp .env.example .env
# Edite o arquivo .env e defina SA_PASSWORD e JWT_SECRET_KEY com valores seguros

# Subir todos os containers (API + SQL Server)
docker compose up --build
```

Aguarde o SQL Server iniciar e as migrations serem aplicadas automaticamente.
Acesse o Swagger em: `http://localhost:8080/swagger`

Para encerrar:

```bash
docker compose down
```

Para encerrar e remover os dados do banco:

```bash
docker compose down -v
```

### Credenciais padrão

| Campo | Valor |
|-------|-------|
| Email | admin@carrepairshop.com |
| Senha | Admin@123 |

### Variáveis de ambiente

As credenciais sensíveis são gerenciadas via arquivo `.env` (não versionado). Copie `.env.example` para `.env` e defina:

| Variável | Descrição |
|----------|-----------|
| `SA_PASSWORD` | Senha do SA do SQL Server (deve atender aos requisitos de complexidade do SQL Server) |
| `JWT_SECRET_KEY` | Chave secreta para geração de tokens JWT (mínimo 32 caracteres) |

## Arquitetura

Este projeto implementa uma API RESTful para uma oficina mecânica usando **DDD (Domain-Driven Design)** com as seguintes camadas:

- **CarRepairShop.Domain** — Entidades, enumerações e interfaces de domínio
- **CarRepairShop.Repository** — Implementação com EF Core + SQL Server, Code-First migrations
- **CarRepairShop.Services** — Serviços de domínio (hashing de senha, JWT)
- **CarRepairShop.Application** — CQRS com MediatR, validação com FluentValidation
- **CarRepairShop.API** — Controllers ASP.NET Core, autenticação JWT

## Padrões Utilizados

- **DDD** — Separação por camadas com fronteiras bem definidas
- **CQRS** — Commands e Queries separados com MediatR
- **SOLID** — Interfaces para inversão de dependência entre camadas
- **Repository + Unit of Work** — Abstração de acesso a dados
- **Pipeline Behavior** — Validação automática antes dos handlers (fail-fast)

## Configuração

### 1. Secrets (Desenvolvimento)

Use o **dotnet user-secrets** para configurar credenciais localmente sem expô-las em controle de versão:

```bash
cd src/CarRepairShop.API

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=CarRepairShopDb;User Id=sa;Password=SuaSenhaForte!;TrustServerCertificate=True;"
dotnet user-secrets set "JwtSettings:SecretKey" "SuaChaveSecretaComPeloMenos32Caracteres!"
```

### 2. Migrations

```bash
# Aplicar migrations e criar o banco de dados
dotnet ef database update \
  --project src/CarRepairShop.Repository \
  --startup-project src/CarRepairShop.API
```

A primeira migration já inclui o seeding do usuário administrador:

| Campo | Valor |
|-------|-------|
| Email | admin@carrepairshop.com |
| Senha | Admin@123 |
| Role  | Admin |

> ⚠️ Altere a senha do administrador após o primeiro login em produção.

### 3. Executar a API

```bash
cd src/CarRepairShop.API
dotnet run
```

Acesse o Swagger em: `https://localhost:7xxx/swagger`

## Autenticação

1. `POST /api/auth/login` com `{ "email": "admin@carrepairshop.com", "password": "Admin@123" }`
2. Copie o token JWT retornado
3. No Swagger, clique em **Authorize** e informe `Bearer <token>`

## Endpoints Principais

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/auth/login` | Autenticação |
| GET/POST | `/api/customers` | Clientes |
| GET/PUT/DELETE | `/api/customers/{id}` | Cliente por ID |
| GET/POST | `/api/vehicles` | Veículos |
| GET/POST | `/api/serviceorders` | Ordens de serviço |
| POST | `/api/serviceorders/{id}/items` | Adicionar item à OS |
| PATCH | `/api/serviceorders/{id}/status` | Atualizar status da OS |
| GET/POST | `/api/users` | Usuários (Admin) |
