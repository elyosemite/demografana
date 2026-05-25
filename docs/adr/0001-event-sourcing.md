# Event Sourcing como estratégia de persistência

O estado de um `Order` não é armazenado diretamente — é derivado do replay de eventos imutáveis no Event Store (`order_events`). Escolhemos event sourcing porque o objetivo central do sistema é observabilidade: cada transição de estado (`OrderPlaced`, `OrderConfirmed`, etc.) é um fato de negócio que precisa aparecer em logs estruturados e traces com contexto completo. Uma tabela de estado corrente perderia esse histórico.

## Considered Options

- **Estado corrente em tabela `orders`** — rejeitado porque apaga o histórico de transições, que é exatamente o dado mais valioso para análise de observabilidade.
- **Event sourcing com EventStoreDB** — rejeitado para evitar dependência de infraestrutura adicional; PostgreSQL com `UNIQUE (aggregate_id, version)` é suficiente para concorrência otimista e replay.
