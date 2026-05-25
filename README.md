# demografana

Projeto de estudo com foco em **observabilidade** — geração e análise de logs estruturados, traces distribuídos e métricas usando a stack OpenTelemetry + Grafana (Loki, Tempo, Alloy).

O domínio é simples de propósito: um ciclo de vida de pedidos (`Order`) serve apenas como pretexto para produzir telemetria rica o suficiente para explorar as ferramentas.

## Arquitetura

```
┌─────────────┐   HTTP    ┌──────────────────────────────────┐
│   Cliente   │ ────────► │        demografana-api            │
└─────────────┘           │  Minimal API · Event Store (PG)  │
                          └──────────┬───────────────────────┘
                                     │ order_events (published_at IS NULL)
                          ┌──────────▼───────────────────────┐
                          │       demografana-relay           │
                          │  Outbox Relay · 500ms polling    │
                          └──────────┬───────────────────────┘
                                     │ RabbitMQ (topic exchange)
                          ┌──────────▼───────────────────────┐
                          │       demografana-worker          │
                          │  5 consumers · atualiza orders   │
                          └──────────────────────────────────┘
```

Todos os containers exportam traces e logs via OTLP → Alloy → Loki / Tempo → Grafana.

## Rodando com imagens do registry

As imagens são publicadas automaticamente no GitHub Container Registry a cada push em `main`. Não é necessário clonar o repositório para subir o ambiente.

**1. Baixe as imagens:**

Última versão estável:

```sh
docker pull ghcr.io/elyosemite/demografana/api:latest
docker pull ghcr.io/elyosemite/demografana/relay:latest
docker pull ghcr.io/elyosemite/demografana/worker:latest
```

Ou uma versão específica:

```sh
docker pull ghcr.io/elyosemite/demografana/api:v1.4.5
docker pull ghcr.io/elyosemite/demografana/relay:v1.4.5
docker pull ghcr.io/elyosemite/demografana/worker:v1.4.5
```

**2. Baixe o `docker-compose.yml`:**

```sh
curl -O https://raw.githubusercontent.com/elyosemite/demografana/main/docker-compose.yml
```

Baixe também os arquivos de configuração da observabilidade:

```sh
curl --create-dirs -O --output-dir observability/alloy \
  https://raw.githubusercontent.com/elyosemite/demografana/main/observability/alloy/config.alloy
curl --create-dirs -O --output-dir observability/loki \
  https://raw.githubusercontent.com/elyosemite/demografana/main/observability/loki/loki.yaml
curl --create-dirs -O --output-dir observability/tempo \
  https://raw.githubusercontent.com/elyosemite/demografana/main/observability/tempo/tempo.yaml
```

**3. Suba o ambiente:**

```sh
docker-compose up
```

Os containers sobem na ordem: PostgreSQL → RabbitMQ → API → Relay → Worker → stack de observabilidade.

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/orders` | Lista todos os pedidos |
| `POST` | `/orders` | Cria um pedido (`Pending`) |
| `POST` | `/orders/{id}/confirm` | `Pending → Confirmed` |
| `POST` | `/orders/{id}/ship` | `Confirmed → Shipped` |
| `POST` | `/orders/{id}/deliver` | `Shipped → Delivered` |
| `POST` | `/orders/{id}/cancel` | Cancela (qualquer estado antes de `Shipped`) |

Transições inválidas retornam `409 Conflict`.

Documentação interativa disponível em `http://localhost:8080/scalar` após subir o ambiente.

## Observabilidade

| Ferramenta | URL local | Finalidade |
|---|---|---|
| Grafana | `http://localhost:3000` | Dashboards, Loki, Tempo |
| RabbitMQ | `http://localhost:15672` | Gerenciamento de filas (guest/guest) |

## Construindo localmente

Requer .NET 10 SDK e Docker.

```sh
git clone https://github.com/elyosemite/demografana.git
cd demografana
dotnet build demografana.slnx
docker-compose up --build
```

## Projetos

| Projeto | Tipo | Responsabilidade |
|---|---|---|
| `demografana.Core` | classlib | Aggregate Root, Domain Events, EventStore, AppDbContext |
| `demografana.Api` | web | Endpoints HTTP, 6 use cases, publisher MassTransit |
| `demografana.Relay` | worker | Outbox Relay — Event Store → RabbitMQ |
| `demografana.Worker` | worker | 5 consumers — RabbitMQ → Projection (read model) |
