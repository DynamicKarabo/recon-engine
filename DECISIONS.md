# Architecture Decision Records — Reconciliation Engine

<!--
Statuses: Proposed | Accepted | Deprecated | Superseded
Dates are ISO 8601 (YYYY-MM-DD).
-->

---

## ADR-001: Clean Architecture

- **Date**: 2025-11-15
- **Status**: Accepted

### Context

The reconciliation engine needs to integrate with multiple external services (Azure Service Bus, Key Vault, SQL Server, ML service) while keeping the core business logic decoupled from infrastructure concerns. Future migration between cloud providers or database engines should not require rewriting domain logic.

### Decision

Adopt **Clean Architecture** (ports & adapters) with strict dependency inversion:

```
Domain (zero dependencies)
  ← Application (depends on Domain)
    ← Infrastructure (depends on Application + Domain)
      ← API (depends on Application + Infrastructure)
```

- The **Domain** layer contains only entities, enums, and events — no framework attributes, no NuGet packages.
- The **Application** layer defines interfaces (ports) for all external concerns — repositories, encryption, event publishing, ML scoring.
- The **Infrastructure** layer implements those interfaces using specific technologies (EF Core, Azure SDK, HTTP).
- The **API** layer composes the application and handles HTTP concerns.

### Consequences

**Positive:**
- Domain logic is testable without any infrastructure setup — pure C# assertions
- Swapping Azure Service Bus for RabbitMQ requires only a new `IEventPublisher` implementation
- EF Core version upgrades or SQL Server → PostgreSQL migration is confined to Infrastructure

**Negative:**
- More boilerplate — every external dependency needs an interface and an implementation
- Initial setup time is higher than a vertical-slice or flat architecture

### Alternatives Considered

| Alternative | Why Rejected |
|-------------|--------------|
| **Vertical Slices** | Better for read-heavy or CRUD apps; our matching pipeline is write-heavy with complex domain logic that benefits from isolation |
| **Flat / Simple 3-Tier** | Faster to build initially but leads to tight coupling between business logic and infrastructure over time |
| **Modular Monolith (feature folders)** | Works at small scale but doesn't enforce dependency direction — infrastructure leaks into domain over time |

---

## ADR-002: CQRS with MediatR

- **Date**: 2025-11-15
- **Status**: Accepted

### Context

The system has two distinct command flows: transaction ingestion and matching pipeline execution. These share the same domain model but have different validation, side effects, and success/failure paths. We need a pattern that separates these commands cleanly without coupling controllers to handler implementations.

### Decision

Use **CQRS** (Command Query Responsibility Segregation) via **MediatR 12**:

- Commands are plain C# records implementing `IRequest<TResponse>`
- Handlers are dedicated classes implementing `IRequestHandler<TRequest, TResponse>`
- MediatR dispatches requests through a pipeline that includes `ValidationBehavior<TRequest, TResponse>`
- The pipeline behavior runs FluentValidation before the handler executes

**Current commands:**
- `IngestTransactionCommand` / `IngestTransactionCommandHandler`
- `MatchingPipelineCommand` / `MatchingPipelineCommandHandler`

### Consequences

**Positive:**
- Controllers stay thin — one `_mediator.Send(command)` call per action
- Validation is decoupled from handlers via pipeline behavior
- New commands can be added without modifying existing handlers or controllers
- Handlers can be unit-tested independently by injecting mocked interfaces

**Negative:**
- MediatR adds a layer of indirection — harder to trace the execution path at a glance
- Registering all handlers via assembly scanning can obscure DI registration

### Alternatives Considered

| Alternative | Why Rejected |
|-------------|--------------|
| **Direct DI (controller → service)** | Works but doesn't enforce the command/handler pattern; services tend to grow into god classes |
| **Domain Events only** | Good for propagating side effects but doesn't handle request-response well |
| **No CQRS (CRUD services)** | Fine for simple CRUD; our matching pipeline has complex orchestration that benefits from dedicated handlers |

---

## ADR-003: Database & Storage

- **Date**: 2025-11-15
- **Status**: Accepted

### Context

Financial reconciliation requires ACID transactions, complex queries (candidate matching across transactions), and append-only audit logs. The data model involves multiple aggregate types with relationships (transactions, reconciliation records, exception records, audit log entries).

### Decision

Use **SQL Server** as the primary database with **EF Core 8.0** as the ORM.

**Key schema decisions:**
- Unique constraint on `Transactions(Source, ExternalId)` for idempotent ingestion
- Composite primary key on `ReconciliationRecordTransactions(RecordId, TransactionId)` for the many-to-many join
- `ExceptionStatus` and `TransactionStatus` stored as strings (readable in the DB, no magic numbers)
- `MatchMethod` stored as string for query readability
- Audit log stored as a flat table with JSON snapshots (`PreviousState`, `NewState` as `nvarchar(max)`)
- Precision on decimal fields: `Amount` → `(18,4)`, `ConfidenceScore` → `(5,4)`

### Consequences

**Positive:**
- EF Core provides compile-time query safety, migrations, and strong typing
- SQL Server works well with Hangfire (which uses the same connection)
- String-based enums make the database self-documenting
- Unique constraint guarantees idempotency without application-level locking

**Negative:**
- SQL Server licensing costs for production
- EF Core query performance needs monitoring for the matching candidate queries (full table scans on pending transactions)
- Migration management requires discipline in CI/CD

### Alternatives Considered

| Alternative | Why Rejected |
|-------------|--------------|
| **PostgreSQL** | Excellent choice; SQL Server chosen for existing team expertise and Azure ecosystem alignment (Azure SQL, Hangfire compatibility) |
| **MongoDB / NoSQL** | Relational integrity (unique constraints, foreign keys, joins) is essential for reconciliation — NoSQL would force application-level consistency |
| **Dapper** | Less abstraction but more control; EF Core's change tracking and configurable entity configurations outweigh the overhead for this domain model |

---

## ADR-004: Testing Strategy

- **Date**: 2025-11-15
- **Status**: Accepted

### Context

Financial reconciliation software requires high correctness guarantees. Incorrect matches or missed exceptions have direct monetary impact. The system integrates with multiple external services that are expensive or impossible to run in CI.

### Decision

Adopt a **layered testing strategy** with 72 tests across four categories:

| Layer | Approach | Tools | Count |
|-------|----------|-------|-------|
| **Domain** | Pure unit tests — no infrastructure, no mocking | xUnit + FluentAssertions | 9 |
| **Validation** | FluentValidation `TestValidate` — no controller needed | FluentValidation TestHelper | 16 |
| **Matching** | Unit tests with mocked dependencies for ML/RuleCache | xUnit + Moq | 13 |
| **Integration** | InMemory EF Core database, mocked external services | xUnit + Moq + InMemory | 16 |
| **Infrastructure Security** | Verify encryption, append-only invariants via reflection | xUnit + Moq + InMemory | 5 |
| **Middleware (E2E)** | `DefaultHttpContext` — full middleware pipeline | xUnit + Moq | 7 |

**Testing principles:**
- Domain entities tested in isolation (no mocking, no database)
- Matching strategies tested as pure functions where possible
- Command handlers tested with `UseInMemoryDatabase()` for EF Core
- External services (encryption, event publisher, ML client) always mocked
- No network calls in tests — ML service client is always mocked
- AuditLog append-only invariant verified via reflection on private setters

### Consequences

**Positive:**
- Tests run in CI without any external infrastructure (no SQL Server, no Service Bus)
- High confidence in pipeline correctness — matching edge cases are explicitly tested
- Validation tests verify business rules without controller integration
- Security tests (encryption, append-only) protect against accidental regression

**Negative:**
- InMemory EF Core doesn't enforce the same constraints as real SQL Server (unique indexes, foreign keys are not enforced)
- No integration test that validates the full stack from HTTP to database
- Reliance on mocks means the ML integration is coverage-tested but not end-to-end tested

### Alternatives Considered

| Alternative | Why Rejected |
|-------------|--------------|
| **Testcontainers (real SQL Server in Docker)** | Better fidelity but slower and requires Docker in CI; reserved for future E2E test project |
| **WebApplicationFactory + Testcontainers** | Full integration but currently out of scope for initial release; 72 tests + mocks provides sufficient coverage |
| **Snapshot testing** | High maintenance; the current assertion-based approach is more explicit |

---

## ADR-005: Matching Strategy Pipeline

- **Date**: 2025-11-20
- **Status**: Accepted

### Context

Financial transactions from different sources may match in different ways. Some pairs are exact duplicates (same amount, date, reference). Others are semantically equivalent but differ in formatting (whitespace, case, partial references). Some require configurable business rules, and complex pattern recognition benefits from ML.

The matching system needs to be extensible (new strategies added without modifying existing ones) and efficient (short-circuit on first match).

### Decision

Implement the matching pipeline as a **Strategy Pattern** with **cascading execution**:

```csharp
var strategies = new List<IMatchingStrategy>
{
    new ExactMatchingStrategy(),       // 1. Exact field match
    new FuzzyMatchingStrategy(),       // 2. FuzzySharp ≥ 0.92
    new MLMatchingStrategy(_mlClient), // 3. ML confidence ≥ 0.85
    new RuleBasedMatchingStrategy(_ruleCache)  // 4. Configurable rules
};

foreach (var strategy in strategies)
{
    var result = strategy.TryMatch(transaction, candidates);
    if (result != null) return result; // Short-circuit
}
// No match → create exception record
```

**Strategy order rationale:**
1. **Exact** — fastest, highest confidence (Amount + Currency + Date + Reference)
2. **Fuzzy** — moderate cost, high confidence with reference/description similarity
3. **ML** — expensive (HTTP call), best for complex patterns
4. **Rule-Based** — last resort, configurable business rules

### Consequences

**Positive:**
- New matching strategies can be added by creating a new class implementing `IMatchingStrategy` — no pipeline code change
- Short-circuit behavior minimizes expensive operations (ML HTTP calls) to only when needed
- Each strategy focuses on one matching approach — easy to test, reason about, and optimize
- Rule-based strategies are configurable via database rather than code

**Negative:**
- Order matters — a lower-precision strategy could match before a higher-precision one if order is wrong
- No parallel execution (strategies run sequentially) — acceptable because ML is the only expensive strategy at ~5s timeout
- The pipeline evaluates every pending transaction as a candidate — performance degrades with transaction volume

### Alternatives Considered

| Alternative | Why Rejected |
|-------------|--------------|
| **Parallel execution (all strategies at once)** | Wastes ML calls when exact match would have succeeded; sequential with short-circuit is more efficient |
| **Single scoring model (always use ML)** | ML service may be unavailable or slow; having a fallback path is essential for reliability |
| **Configurable strategy order** | The current hardcoded order is empirically optimal; configurable order adds complexity without demonstrated need |

---

## ADR-006: Event-Driven Integration with Azure Service Bus

- **Date**: 2025-11-20
- **Status**: Accepted

### Context

When a transaction is ingested, matched, or raises an exception, downstream systems need to react — update dashboards, notify counterparties, trigger alerts, or initiate further processing. Using direct HTTP calls to downstream services would create tight coupling and availability dependencies.

### Decision

Publish domain events to **Azure Service Bus** (topic: `reconciliation.transactions`). The `IEventPublisher` interface abstracts the transport:

```csharp
public interface IEventPublisher
{
    Task PublishAsync(TransactionIngestedEvent @event, ...);
    Task PublishAsync(TransactionMatchedEvent @event, ...);
    Task PublishAsync(ExceptionRaisedEvent @event, ...);
}
```

Each event is serialized as JSON with a `Subject` header set to the event type name. Subscribers filter by subject. The envelope includes `correlationId` for distributed tracing.

**Dead-letter handling:**
- A Hangfire recurring job (`DeadLetterMonitorJob`) runs every 5 minutes to peek at the dead-letter queue
- If messages are found, a critical log alert is raised

### Consequences

**Positive:**
- Downstream systems are decoupled from the reconciliation engine — they can be down without affecting ingestion/matching
- New subscribers can be added without modifying the reconciliation engine
- Correlation IDs propagate through events, enabling end-to-end tracing
- Service Bus provides at-least-once delivery guarantees

**Negative:**
- Azure Service Bus adds operational complexity and cost
- Events are fire-and-forget from the engine's perspective — no retry logic for downstream failures
- The dead-letter monitor only alerts; it doesn't automatically reprocess

### Alternatives Considered

| Alternative | Why Rejected |
|-------------|--------------|
| **Direct HTTP callbacks** | Creates tight coupling; downstream downtime blocks our pipeline |
| **In-memory events (MediatR notifications)** | Doesn't cross process boundaries — only works for within-process handlers |
| **Azure Event Grid** | Good for simpler eventing but ASB offers more control (sessions, dead-lettering, scheduled delivery) |
| **Kafka / Event Hubs** | Overkill for current scale; ASB is simpler to manage in Azure ecosystem |

---

## ADR-007: Application-Level Encryption for PII

- **Date**: 2025-11-20
- **Status**: Accepted

### Context

Financial transactions contain sensitive data — account IDs and descriptions — that must be encrypted at rest. The encryption must happen before data reaches the database (defense in depth beyond TDE). Key management must be centralized and auditable.

### Decision

Encrypt PII at the **application layer** using **AES-256-CBC** with keys sourced from **Azure Key Vault**:

```
Client → API → Application Layer → Encrypt(AccountId, Description) → EF Core → SQL Server
```

**Implementation:**
- `IEncryptionService` interface defines `Encrypt()` and `Decrypt()`
- `AzureKeyVaultEncryptionService` fetches a key from Key Vault, derives an AES key via SHA-256
- Key is cached in memory (thread-safe with `SemaphoreSlim`)
- Each encryption uses a random IV prepended to the ciphertext
- Null/empty fields are passed through (no encryption of null)

**Encryption happens in the handler, not the database or ORM layer.** The handler calls `_encryptionService.Encrypt()` before calling `Transaction.Create()`. EF Core persists the ciphertext as-is.

### Consequences

**Positive:**
- PII is encrypted before it ever reaches the database — TDE is defense in depth
- Key rotation via Azure Key Vault doesn't require application redeployment
- Encryption can be tested with mocked `IEncryptionService` in unit tests
- Weakest link is the key cache in memory (acceptable risk for the deployment environment)

**Negative:**
- Encrypted fields cannot be searched, sorted, or indexed by the database
- Decryption requires the key to be loaded (cold start latency on first call)
- Key caching in memory means a memory dump could leak the derived key
- Every new sensitive field requires adding encryption logic

### Alternatives Considered

| Alternative | Why Rejected |
|--------------|--------------|
| **SQL Server Always Encrypted** | Couples encryption to SQL Server; makes index usage on encrypted columns tricky; harder to test |
| **EF Core value converters** | Works well but hides encryption from the code — harder to audit and reason about |
| **No encryption (rely on TDE only)** | Insufficient defense in depth; TDE protects at rest but not from DBAs or backup access |
| **Per-field encryption keys** | Adds complexity without proportionate security gain for the current threat model |
