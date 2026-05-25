# demografana.Relay

Outbox Relay — background service que faz a ponte entre o Event Store (PostgreSQL) e o broker de mensagens (RabbitMQ).

## Responsabilidade

A cada 500ms, lê eventos com `published_at IS NULL` da tabela `order_events`, publica cada um no RabbitMQ via MassTransit e marca `published_at = NOW()`. Garante que nenhum evento chegue ao broker sem ter sido primeiro persistido no banco.

## Fluxo

```
order_events (published_at IS NULL)
  → deserializa payload por event_type
  → IBus.Publish<T>(evento)         ← MassTransit → RabbitMQ
  → order_events.published_at = NOW()
```

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
dotnet run --project demografana.Relay
```

Requer PostgreSQL e RabbitMQ ativos. Não expõe porta HTTP.
