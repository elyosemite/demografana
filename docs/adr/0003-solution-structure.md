# Solução com 4 projetos e Core como shared kernel

O sistema é composto por quatro projetos .NET na mesma solution: `demografana.Core` (shared kernel), `demografana.Api`, `demografana.Relay` e `demografana.Worker`. Api, Relay e Worker são executáveis independentes em containers Docker separados. Escolhemos essa estrutura porque os três executáveis têm ciclos de vida e responsabilidades distintos — separar em containers permite escalar e fazer deploy de cada um independentemente.

## Considered Options

- **Projeto único com `APP_MODE` via variável de ambiente** — rejeitado porque mascara os limites entre Api, Relay e Worker no código, dificultando o crescimento em direção a DDD com bounded contexts bem definidos.

## Consequences

`demografana.Core` é o shared kernel: contém entidades, Domain Events, Aggregate Root, `AppDbContext`, `EventStore` e contratos de mensageria. Nenhum dos três executáveis referencia os outros — todos referenciam apenas Core.
