# demografana.Api

API HTTP da aplicação. Recebe comandos, aplica-os ao Aggregate Root `Order` via `EventStore` e retorna respostas.

## Endpoints

| Método | Rota | Descrição | Resposta |
|---|---|---|---|
| `GET` | `/orders` | Lista todos os pedidos (read model) | `200 OK` |
| `POST` | `/orders` | Cria um novo pedido | `201 Created` |
| `POST` | `/orders/{id}/confirm` | Confirma um pedido (`Pending → Confirmed`) | `204 No Content` |
| `POST` | `/orders/{id}/ship` | Marca como enviado (`Confirmed → Shipped`) | `204 No Content` |
| `POST` | `/orders/{id}/deliver` | Marca como entregue (`Shipped → Delivered`) | `204 No Content` |
| `POST` | `/orders/{id}/cancel` | Cancela um pedido (body: `{ "reason": "..." }`) | `204 No Content` |

Transições inválidas retornam `409 Conflict` com `{ "error": "InvalidTransition", "from": "...", "to": "..." }`.

## Configuração

```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=demografana;..."
  },
  "RabbitMq": { "Host": "localhost", "Username": "guest", "Password": "guest" }
}
```

Variáveis de ambiente sobrescrevem os valores acima (padrão Docker):
```
ConnectionStrings__DefaultConnection=Host=postgres;...
RabbitMq__Host=rabbitmq
OTEL_EXPORTER_OTLP_ENDPOINT=http://alloy:4317
```

## Executar localmente

```sh
dotnet run --project demografana.Api
```

Requer PostgreSQL em `localhost:5432` e RabbitMQ em `localhost:5672`. Migrations são aplicadas automaticamente no startup.

## Observabilidade

Logs exportados via Serilog → OTLP → Alloy → Loki. Traces via OpenTelemetry → Tempo. Enriquecimento: `ThreadId`, `ProcessId`, `MachineName`.
