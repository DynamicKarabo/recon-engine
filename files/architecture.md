# Financial Reconciliation Engine — Architecture

## Architectural Style

- **CQRS via MediatR** — commands and queries separated; all business logic flows through handlers
- **Event-driven async processing** — ingestion and matching decoupled via Azure Service Bus
- **Clean Architecture** — Domain → Application → Infrastructure → API layers; dependencies point inward

---

## Project Structure

```
ReconciliationEngine/
├── ReconciliationEngine.API/              # HTTP layer — controllers, middleware, DI setup
├── ReconciliationEngine.Application/     # MediatR commands, queries, handlers, DTOs
├── ReconciliationEngine.Domain/          # Entities, value objects, domain events, enums
├── ReconciliationEngine.Infrastructure/  # SQL Server, ASB, Key Vault, Hangfire, encryption
└── ReconciliationEngine.Tests/           # Unit and integration tests
```

---

## Layer Responsibilities

### Domain

- `Transaction`, `ReconciliationRecord`, `ExceptionRecord`, `AuditLog`, `MatchingRule` entities
- Enums: `TransactionStatus`, `ExceptionCategory`, `ExceptionStatus`, `MatchMethod`
- Domain events: `TransactionIngested`, `TransactionMatched`, `ExceptionRaised`
- No infrastructure dependencies

### Application

- MediatR commands: `IngestTransactionCommand`, `ResolveExceptionCommand`, `AssignExceptionCommand`
- MediatR queries: `GetTransactionQuery`, `ListExceptionsQuery`, `GetAuditLogQuery`
- Matching pipeline orchestration: `RunMatchingPipelineCommand`
- Interfaces: `ITransactionRepository`, `IReconciliationRepository`, `IAuditLogger`, `IEncryptionService`

### Infrastructure

- SQL Server repositories (EF Core)
- Azure Service Bus publisher and consumer
- Azure Key Vault key management
- Encryption service (AES-256, column-level)
- Hangfire job registration
- External matching libraries (Jaro-Winkler)

### API

- Controllers: `TransactionsController`, `ReconciliationsController`, `ExceptionsController`, `AuditController`, `RulesController`
- Middleware: exception handling, correlation ID propagation, JWT validation
- Hangfire dashboard (secured)

---

## MediatR Flow

### Ingestion (synchronous path)

```
POST /transactions/ingest
    │
    ▼
TransactionsController.Ingest()
    │
    ▼
mediator.Send(IngestTransactionCommand)
    │
    ▼
IngestTransactionCommandHandler
    ├── Check idempotency (Source, ExternalId)
    ├── Encrypt PII fields via IEncryptionService
    ├── Persist Transaction (Status: Pending)
    ├── Write AuditLog (action: Ingested)
    └── Publish TransactionIngested event → Azure Service Bus
    │
    ▼
Return 201 / 200
```

### Matching (async path)

```
Azure Service Bus → TransactionIngested event
    │
    ▼
MatchingPipelineConsumer (ASB consumer / Hangfire job)
    │
    ▼
mediator.Send(RunMatchingPipelineCommand)
    │
    ▼
RunMatchingPipelineCommandHandler
    ├── Idempotency check (skip if already Matched/Exception)
    ├── Step 1: ExactMatchStrategy
    ├── Step 2: FuzzyMatchStrategy (Jaro-Winkler)
    ├── Step 3: RuleBasedMatchStrategy
    │
    ├── Match found:
    │   ├── Create ReconciliationRecord
    │   ├── Update Transactions (Status: Matched)
    │   └── Write AuditLog (action: ReconciliationCreated)
    │
    └── No match:
        ├── Create ExceptionRecord (Category: Unmatched)
        ├── Update Transaction (Status: Exception)
        └── Write AuditLog (action: ExceptionRaised)
```

---

## Azure Service Bus

**Topics and Subscriptions**

| Topic | Publisher | Subscriber | Event |
|---|---|---|---|
| `reconciliation.transactions` | Ingestion handler | Matching pipeline consumer | `TransactionIngested` |

**Message envelope:**
```json
{
  "eventType": "TransactionIngested",
  "transactionId": "3fa85f64-...",
  "source": "BankFeedA",
  "correlationId": "e9d1c3a2-...",
  "occurredAt": "2024-11-01T08:00:00Z"
}
```

**Retry policy:** 3 attempts with exponential backoff. Dead-letter queue monitored via Hangfire health check job.

---

## Hangfire Jobs

| Job | Schedule | Description |
|---|---|---|
| `DeadLetterMonitorJob` | Every 5 min | Checks ASB dead-letter queue; raises alerts |
| `StaleExceptionAlertJob` | Daily 08:00 | Flags exceptions older than 48h with no reviewer |
| `RuleCacheRefreshJob` | Every 10 min | Refreshes in-memory matching rule cache |

Hangfire uses SQL Server as its backing store. Dashboard exposed at `/hangfire` (admin role only).

---

## Azure Key Vault Integration

- Encryption keys for PII fields retrieved from Key Vault at startup and cached in memory
- Key rotation: new key version retrieved on next cache refresh; old ciphertext re-encrypted on next write
- No keys stored in `appsettings.json` or environment variables
- Key Vault access uses Managed Identity in production

---

## Correlation ID Propagation

Every request receives a `CorrelationId` (GUID) at the API layer via middleware. It is:

- Attached to all ASB messages
- Written to every `AuditLog` entry
- Logged in structured logging (Serilog)
- Returned in the `X-Correlation-Id` response header

This enables full tracing of a transaction from ingestion through matching through exception resolution.

---

## Security

- JWT bearer auth on all API endpoints
- Role-based: `Operator` (read + exception actions), `Admin` (rules management, Hangfire dashboard)
- PII fields encrypted at application layer (AES-256) before DB write
- Azure Key Vault for key management (Managed Identity)
- HTTPS enforced; HSTS enabled
- No PII in logs — structured logging masks sensitive fields

---

## POPIA Compliance Notes

- Minimum PII: only store what is necessary for reconciliation
- `AccountId` and sensitive description fields encrypted at rest
- No personal data in error messages or logs
- Audit log supports subject access requests — query by `EntityId`
- Data retention policy to be enforced by a scheduled purge job (v2)
