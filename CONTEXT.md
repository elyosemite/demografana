# Glossário de Domínio — demografana

## Order

Uma solicitação de compra feita por um Customer. Possui um ciclo de vida definido e é a entidade central do sistema.

## Order Status

O estado corrente de um Order dentro do seu ciclo de vida.

Estados válidos e transições permitidas:

```
Pending → Confirmed → Shipped → Delivered
                    ↘ Cancelled  (permitido de qualquer estado anterior a Shipped)
```

## Order Lifecycle

A sequência de transições de status que um Order percorre desde a criação até o encerramento. Cada transição é um evento de negócio observável.

## OrderItem

Uma linha dentro de um Order. Representa um produto específico, a quantidade solicitada e o preço unitário no momento da colocação do pedido.

## Customer

A parte que coloca um Order. Identificada por um `CustomerId` opaco. Não é gerenciada por este sistema.

## Product

Um item que pode ser incluído em um Order. Identificado por um `ProductId` opaco. Não é gerenciado por este sistema.

## Domain Event

Um fato imutável que aconteceu no sistema. É a unidade de escrita do Event Store. Cada evento corresponde a um endpoint de API dedicado:

| Evento            | Endpoint                          |
|-------------------|-----------------------------------|
| Evento            | Endpoint                          | Payload                                              |
|-------------------|-----------------------------------|------------------------------------------------------|
| `OrderPlaced`     | `POST /orders`                    | `customerId`, `items[]` (productId, qty, unitPrice), `total` |
| `OrderConfirmed`  | `POST /orders/{id}/confirm`       | _(vazio)_                                            |
| `OrderShipped`    | `POST /orders/{id}/ship`          | _(vazio)_                                            |
| `OrderDelivered`  | `POST /orders/{id}/deliver`       | _(vazio)_                                            |
| `OrderCancelled`  | `POST /orders/{id}/cancel`        | `reason` (obrigatório)                               |

## Event Store

Tabela append-only no PostgreSQL que persiste todos os Domain Events de um Order em ordem. Colunas centrais: `aggregate_id`, `version`, `event_type`, `payload` (jsonb), `occurred_at`. É a fonte de verdade do sistema.

## Solution Structure

Quatro projetos .NET na mesma solution:

| Projeto | Responsabilidade |
|---|---|
| `demografana.Core` | Shared kernel: entidades, Domain Events, Aggregate Root, AppDbContext, EventStore, contratos de mensageria |
| `demografana.Api` | Endpoints HTTP, Use Cases |
| `demografana.Relay` | Outbox Relay (IHostedService) |
| `demografana.Worker` | Projection Worker (IHostedService) |

Dependências: Api, Relay e Worker referenciam Core. Nenhum referencia os outros.

## Aggregate Root

`Order` é o Aggregate Root do sistema. Encapsula toda a lógica de validação de transições e produção de Domain Events. O padrão de uso é: carregar eventos do Event Store → reconstruir estado via replay → executar comando → coletar eventos pendentes → persistir no Event Store. Use Cases orquestram; o `Order` decide.

## Projection

O estado atual de um Order, derivado do replay dos seus Domain Events. Armazenada na tabela `orders` como read model. Não é fonte de verdade — pode ser reconstruída a partir do Event Store. Atualizada de forma assíncrona por um Projection Worker via Outbox.

## Outbox

Implementado como coluna `projected_at` na própria tabela do Event Store. Eventos com `projected_at IS NULL` são pendentes de projeção. O Projection Worker marca `projected_at = NOW()` após atualizar a Projection com sucesso. Elimina duplicação de payload e fornece uma métrica de lag grátis (`NOW() - projected_at`).

## Outbox Relay

Background service que lê eventos com `published_at IS NULL` do Event Store e os publica no RabbitMQ, marcando `published_at = NOW()` após confirmação do broker. Garante que nenhum evento chegue ao RabbitMQ sem ter sido primeiro persistido no PostgreSQL.

Topologia RabbitMQ:
- Exchange: `order.events` (type: `topic`, durable)
- Queue: `order.projection` (durable)
- Routing key: `order.{event_type}` — ex: `order.OrderPlaced`, `order.OrderCancelled`

O exchange `topic` permite adicionar novos consumers (notificações, analytics) sem alterar o Relay.

## Projection Worker

Background service (IHostedService) que consome mensagens do RabbitMQ, aplica o Domain Event à Projection e marca `projected_at = NOW()` no Event Store. É a única parte do sistema que escreve na tabela `orders`. Deve ser idempotente — pode receber o mesmo evento mais de uma vez em cenários de reprocessamento.

## Invalid Transition

Tentativa de mover um Order para um status que não é permitido a partir do estado atual (ex: `Cancelled → Shipped`). Resulta em `HTTP 409 Conflict` com detalhe da transição (`from`, `to`). Logado como `Warning` com `orderId`, `from` e `to` para rastreabilidade em logs e traces.

## Observability

O objetivo central do sistema hoje. Abrange logs estruturados ricos (eventos de negócio + contexto de runtime: thread, máquina, processo), distributed tracing e métricas — exportados via OpenTelemetry para a stack Grafana (Loki, Tempo, Alloy).
