# Order Processing POC — Event Driven Architecture (.NET + SQS + Outbox)

## 📊 Diagrama


<img width="1757" height="899" alt="architecture" src="https://github.com/user-attachments/assets/3899ad28-da31-47e3-9606-c14b4a3b4b6e" />



POC de processamento de ordens de compra simulando fluxo de corretora, com foco em:

- idempotência
- consistência entre persistência e mensageria
- processamento assíncrono
- reserva e compensação de saldo
- resiliência e observabilidade

O objetivo foi exercitar decisões reais de arquitetura distribuída e demonstrar trade-offs comuns em sistemas financeiros.

---

## 🧠 Arquitetura

Fluxo simplificado:

Client → Order API  
→ validação + antifraude + reserva de saldo  
→ persistência + Outbox (transação única)  
→ Outbox Worker → SQS (order-queue)  
→ Order Consumer → simulação integração B3  
→ atualização status da ordem  
→ publicação evento de liquidação → SQS (balance-queue)  
→ Balance Consumer → compensação de saldo

---

## 🎯 Decisões Arquiteturais

### Outbox Pattern
Garantir consistência entre banco e mensageria sem transações distribuídas.

**Trade-off**
- ✔ elimina risco de perder evento
- ❌ adiciona latência e complexidade operacional

---

### Processamento Assíncrono
Separação entre aceitação da ordem e liquidação.

**Trade-off**
- ✔ maior resiliência e escalabilidade
- ✔ absorção de picos
- ❌ consistência eventual
- ❌ maior complexidade de debugging

---

### Idempotência
Evita duplicidade em cenários de retry de cliente ou gateway.

**Trade-off**
- ✔ segurança operacional
- ❌ custo extra de persistência e lookup

---

### Reserva de saldo + compensação (mini saga)
Evita inconsistência entre ordem executada e saldo disponível.

**Trade-off**
- ✔ integridade financeira
- ✔ isolamento entre domínios
- ❌ necessidade de compensações e monitoramento

---

### Retry natural via fila
Mensagens não deletadas são reprocessadas automaticamente.

**Trade-off**
- ✔ simplicidade de retry
- ❌ necessidade de idempotência no consumer

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

### 1) Subir infraestrutura (filas e banco de dados)

```bash
docker compose up -d
```

## 2) Criar as filas

```bash
docker run --rm -it \
  --network host \
  -e AWS_ACCESS_KEY_ID=test \
  -e AWS_SECRET_ACCESS_KEY=test \
  -e AWS_DEFAULT_REGION=sa-east-1 \
  amazon/aws-cli \
  --endpoint-url=http://localhost:4566 \
  sqs create-queue --queue-name order-queue

docker run --rm -it \
  --network host \
  -e AWS_ACCESS_KEY_ID=test \
  -e AWS_SECRET_ACCESS_KEY=test \
  -e AWS_DEFAULT_REGION=sa-east-1 \
  amazon/aws-cli \
  --endpoint-url=http://localhost:4566 \
  sqs create-queue --queue-name balance-queue
  ```
 ## 3) Rodar a aplicação
```bash
dotnet run
```

## 4) Testar o fluxo

Acesse a documentação Swagger para criar ordens de compra:
http://localhost:5233/swagger

## 🧪 Teste de criação de ordem:

```json
POST /api/orders
{
"amount": 1000,
"asset": "PETR4",
"type": "BUY"
}
```

```bash
Idempotency-Key: qualquer-string-unica
Authorization: Bearer fake-token
```
