# demografana.Worker

Projection Worker — consome eventos do RabbitMQ e atualiza o read model (`orders`) no PostgreSQL.

## Responsabilidade

Para cada `Domain Event` consumido via MassTransit, o consumer correspondente:
1. Atualiza a `OrderProjection` na tabela `orders`
2. Marca `projected_at = NOW()` no registro de `order_events`

O Worker é **idempotente** — se receber o mesmo evento duas vezes, a segunda execução é ignorada com segurança (checagem de existência antes de inserir).

## Consumers

| Consumer | Evento | Ação na Projection |
|---|---|---|
| `OrderPlacedConsumer` | `OrderPlaced` | Cria o registro em `orders` |
| `OrderConfirmedConsumer` | `OrderConfirmed` | `Status = Confirmed`, preenche `ConfirmedAt` |
| `OrderShippedConsumer` | `OrderShipped` | `Status = Shipped`, preenche `ShippedAt` |
| `OrderDeliveredConsumer` | `OrderDelivered` | `Status = Delivered`, preenche `DeliveredAt` |
| `OrderCancelledConsumer` | `OrderCancelled` | `Status = Cancelled`, preenche `CancelledAt` e `CancelReason` |

## Configuração

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=demografana;..."
  },
  "RabbitMq": { "Host": "localhost", "Username": "guest", "Password": "guest" },
  "OTEL_EXPORTER_OTLP_ENDPOINT": "http://localhost:4317"
}
```

## Executar localmente

```sh
dotnet run --project demografana.Worker
```

Requer PostgreSQL e RabbitMQ ativos. Não expõe porta HTTP.
