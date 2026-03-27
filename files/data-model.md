# Financial Reconciliation Engine — Data Model

## Design Principles

- No soft deletes — records are immutable once written
- Audit log is append-only; state changes are logged, never overwritten
- PII fields (e.g. account holder names) are encrypted at column level
- All sensitive encryption keys managed via Azure Key Vault
- Timestamps are UTC throughout

---

## Tables

### `Transactions`

Stores all ingested financial transactions. Idempotency enforced on `(Source, ExternalId)`.

| Column | Type | Notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | PK, generated on insert |
| `Source` | `NVARCHAR(100)` | Originating system (e.g. "BankFeedA", "ERP") |
| `ExternalId` | `NVARCHAR(255)` | ID from source system |
| `Amount` | `DECIMAL(18,4)` | Transaction amount |
| `Currency` | `CHAR(3)` | ISO 4217 currency code |
| `TransactionDate` | `DATE` | Business date of transaction |
| `Description` | `NVARCHAR(500)` | Raw description; encrypted if contains PII |
| `Reference` | `NVARCHAR(255)` | Payment reference / memo |
| `AccountId` | `NVARCHAR(255)` | Encrypted — internal account identifier |
| `Status` | `NVARCHAR(50)` | `Pending`, `Matched`, `Exception` |
| `IngestedAt` | `DATETIME2` | UTC timestamp of ingestion |
| `IngestedBy` | `NVARCHAR(100)` | Service or user that ingested |

**Unique constraint:** `(Source, ExternalId)` — enforces idempotency at DB level

---

### `ReconciliationRecords`

Represents a confirmed match between two or more transactions.

| Column | Type | Notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | PK |
| `MatchedAt` | `DATETIME2` | UTC timestamp of match |
| `MatchMethod` | `NVARCHAR(50)` | `Exact`, `Fuzzy`, `RuleBased` |
| `ConfidenceScore` | `DECIMAL(5,4)` | 0.0000–1.0000; null for exact matches |
| `RuleId` | `NVARCHAR(100)` | Nullable; which rule matched (if RuleBased) |
| `CreatedAt` | `DATETIME2` | UTC |

---

### `ReconciliationRecordTransactions`

Junction table linking matched transactions to a reconciliation record.

| Column | Type | Notes |
|---|---|---|
| `ReconciliationRecordId` | `UNIQUEIDENTIFIER` | FK → `ReconciliationRecords.Id` |
| `TransactionId` | `UNIQUEIDENTIFIER` | FK → `Transactions.Id` |

**Composite PK:** `(ReconciliationRecordId, TransactionId)`

---

### `ExceptionRecords`

Unmatched or ambiguous transactions surfaced for manual review.

| Column | Type | Notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` | PK |
| `TransactionId` | `UNIQUEIDENTIFIER` | FK → `Transactions.Id` |
| `Category` | `NVARCHAR(50)` | `Mismatch`, `Duplicate`, `Unmatched` |
| `Status` | `NVARCHAR(50)` | `PendingReview`, `UnderReview`, `Resolved`, `Dismissed` |
| `AssignedTo` | `NVARCHAR(255)` | Nullable; reviewer identity |
| `Notes` | `NVARCHAR(2000)` | Reviewer notes; encrypted if contains PII |
| `ResolvedAt` | `DATETIME2` | Nullable; UTC |
| `CreatedAt` | `DATETIME2` | UTC |

---

### `AuditLog`

Append-only. Every state change across all entities is written here. Never updated or deleted.

| Column | Type | Notes |
|---|---|---|
| `Id` | `BIGINT IDENTITY` | PK |
| `EntityType` | `NVARCHAR(100)` | e.g. `Transaction`, `ExceptionRecord` |
| `EntityId` | `UNIQUEIDENTIFIER` | FK (logical, not enforced) |
| `Action` | `NVARCHAR(100)` | e.g. `Ingested`, `Matched`, `ExceptionRaised`, `Resolved` |
| `PreviousState` | `NVARCHAR(MAX)` | JSON snapshot before change; nullable |
| `NewState` | `NVARCHAR(MAX)` | JSON snapshot after change |
| `PerformedBy` | `NVARCHAR(255)` | Service name or user identity |
| `PerformedAt` | `DATETIME2` | UTC |
| `CorrelationId` | `UNIQUEIDENTIFIER` | Traces across async boundaries |

---

### `MatchingRules`

Configurable rule definitions for the rule-based matching step.

| Column | Type | Notes |
|---|---|---|
| `Id` | `NVARCHAR(100)` | PK; human-readable rule key |
| `Description` | `NVARCHAR(500)` | What the rule does |
| `Priority` | `INT` | Evaluation order; lower = higher priority |
| `IsActive` | `BIT` | Soft-disable without deleting |
| `ConfigJson` | `NVARCHAR(MAX)` | Rule parameters as JSON |
| `CreatedAt` | `DATETIME2` | UTC |
| `UpdatedAt` | `DATETIME2` | UTC |

---

## Encryption Strategy

Fields marked as encrypted use SQL Server Always Encrypted or application-layer AES-256 encryption. Keys are stored in and retrieved from Azure Key Vault at runtime — never in config files or environment variables.

Encrypted columns: `AccountId`, `Description` (when PII), `Notes` (exception reviewer notes)

---

## Indexes

- `Transactions(Source, ExternalId)` — unique, idempotency check
- `Transactions(Status)` — pipeline consumer filtering
- `Transactions(TransactionDate)` — date-range queries
- `ExceptionRecords(Status)` — reviewer queue filtering
- `AuditLog(EntityId, EntityType)` — entity history lookup
- `AuditLog(CorrelationId)` — distributed trace lookup
