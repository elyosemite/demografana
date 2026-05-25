# demografana.Core

Shared kernel da solução. Contém todo o código compartilhado entre `Api`, `Relay` e `Worker`.

## Estrutura

```
Domain/
├── Order.cs                  # Aggregate Root — valida transições, produz eventos
├── OrderItem.cs
├── OrderStatus.cs            # enum: Pending, Confirmed, Shipped, Delivered, Cancelled
├── Events/
│   ├── OrderEvent.cs         # record base abstrato
│   ├── OrderPlaced.cs
│   ├── OrderConfirmed.cs
│   ├── OrderShipped.cs
│   ├── OrderDelivered.cs
│   └── OrderCancelled.cs
└── Exceptions/
    ├── InvalidOrderTransitionException.cs
    └── OrderNotFoundException.cs

Infrastructure/
├── AppDbContext.cs            # EF Core — tabelas order_events e orders
├── EventStore.cs             # append e load de eventos do aggregate
├── OrderEventRecord.cs       # entidade EF para a tabela order_events
└── OrderProjection.cs        # entidade EF para a tabela orders (read model)

Migrations/                   # geradas com: dotnet ef migrations add <Name>
                              #   --project demografana.Core
                              #   --startup-project demografana.Api
```

## Schema

| Tabela | Finalidade |
|---|---|
| `order_events` | Event Store — fonte de verdade. Campos: `aggregate_id`, `version`, `event_type`, `payload` (JSON), `occurred_at`, `published_at`, `projected_at` |
| `orders` | Read model (Projection) — atualizado pelo Worker via `projected_at` |

## Dependências

- `Microsoft.EntityFrameworkCore` 9.x + `Npgsql.EntityFrameworkCore.PostgreSQL` 9.x
- `MassTransit` 8.x (contratos de mensagem compartilhados)
