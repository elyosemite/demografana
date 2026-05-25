# Projeção assíncrona via RabbitMQ com Outbox Relay dedicado

A `Projection` (read model `orders`) é atualizada de forma assíncrona por um `Projection Worker` que consome mensagens do RabbitMQ. Um `Outbox Relay` dedicado lê eventos com `published_at IS NULL` do Event Store e os publica no broker, garantindo que nenhum evento chegue ao RabbitMQ sem ter sido primeiro persistido no PostgreSQL. Escolhemos RabbitMQ (sobre alternativas in-process) porque o sistema roda em containers separados e precisa de escala horizontal.

## Considered Options

- **Projeção síncrona (mesma transação)** — rejeitada porque acopla escrita e leitura e não gera spans de trace independentes para cada etapa do pipeline.
- **`Channel<T>` in-process** — rejeitado porque não funciona entre containers separados.
- **Polling puro sem broker** — rejeitado em favor de RabbitMQ para suportar múltiplos consumers futuros (notificações, analytics) sem alterar o Relay.

## Consequences

O `Projection Worker` deve ser idempotente — pode receber o mesmo evento mais de uma vez em cenários de reprocessamento. A latência entre um evento gravado e sua Projection atualizada é mensurável via `projected_at - occurred_at`, exposta como métrica de lag.
