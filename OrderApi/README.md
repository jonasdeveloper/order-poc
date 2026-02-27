Init readme
# Order Processing POC (.NET + AWS SQS + Outbox Pattern)

POC de processamento de ordens de compra simulando fluxo de corretora/fintech com foco em:

- idempotência
- consistência entre banco e mensageria (Outbox Pattern)
- processamento assíncrono via fila
- reserva e compensação de saldo
- observabilidade e rastreabilidade

O objetivo foi exercitar decisões de arquitetura distribuída e trade-offs comuns em sistemas financeiros.

---

## 🧠 Arquitetura

Fluxo principal:

Cliente → Order API → Reserva saldo → Persistência + Outbox  
Outbox Worker → SQS (order-queue)  
Order Consumer → simulação B3 → atualização status  
Order Consumer → SQS (balance-queue)  
Balance Consumer → compensação de saldo

Principais padrões aplicados:

- Outbox Pattern
- Idempotência na entrada
- Consistência eventual
- Processamento orientado a eventos
- Retry natural via SQS visibility timeout

---

## 🧱 Stack

- .NET 8
- Entity Framework Core
- PostgreSQL
- AWS SQS (LocalStack)
- Serilog
- OpenTelemetry

---

## 🚀 Como rodar local

### 1) Subir infraestrutura

```bash
docker compose up -d