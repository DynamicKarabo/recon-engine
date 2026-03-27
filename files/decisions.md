# Financial Reconciliation Engine — Architecture Decisions

A running log of key decisions made during design. Feed this into NotebookLM alongside the other specs so future queries are grounded in the *why*, not just the *what*.

---

## ADR-001 — Idempotency enforced at `(Source, ExternalId)` level

**Decision:** Duplicate detection uses a unique DB constraint on `(Source, ExternalId)` rather than application-layer checking alone.

**Reason:** Application-layer checks are not safe under concurrent ingestion. A DB unique constraint is atomic. On conflict, we return the existing record with `200 OK` rather than erroring.

---

## ADR-002 — Matching pipeline is async via Azure Service Bus

**Decision:** Transaction ingestion is synchronous (HTTP in, record written, event published). Matching runs asynchronously via ASB consumer.

**Reason:** Matching can be slow (fuzzy scoring, rule evaluation over many candidates). Blocking the HTTP request for matching would degrade ingestion throughput and create timeout risk for batch scenarios.

---

## ADR-003 — Append-only audit log

**Decision:** `AuditLog` table has no `UPDATE` or `DELETE` path. State changes are always new rows.

**Reason:** Compliance and POPIA traceability requirements. Audit logs must be tamper-evident. Any state can be reconstructed by replaying the log.

---

## ADR-004 — Jaro-Winkler for fuzzy matching (not Levenshtein)

**Decision:** Jaro-Winkler similarity used for reference/description fuzzy matching.

**Reason:** Jaro-Winkler weights prefix matches more heavily, which is well-suited to financial references (e.g. "INV-9981" vs "INV-9981-A"). It is also more performant on short strings than Levenshtein edit distance.

---

## ADR-005 — Rule-based matching is DB-driven and hot-reloadable

**Decision:** Matching rules are stored in `MatchingRules` table and cached with a configurable TTL. Rules can be added/modified without a deployment.

**Reason:** Business matching logic changes frequently as new source systems are onboarded. Requiring a code deployment for each rule change is impractical.

---

## ADR-006 — Encryption at application layer, not DB layer

**Decision:** PII fields are encrypted by the application (AES-256) before being written to SQL Server, rather than using SQL Server Always Encrypted or Transparent Data Encryption alone.

**Reason:** Always Encrypted has limitations with EF Core and LINQ queries. Application-layer encryption gives us full control over which fields are encrypted, key rotation behaviour, and is Key Vault agnostic. TDE protects at-rest media but not against a compromised DB connection.

---

## ADR-007 — MediatR for all business logic dispatch

**Decision:** No business logic in controllers. Controllers translate HTTP to MediatR commands/queries and return results.

**Reason:** Keeps controllers thin and testable. Enables cross-cutting pipeline behaviours (validation, logging, performance tracking) via MediatR pipeline behaviours without touching handler code.

---

## ADR-008 — Hangfire backed by SQL Server (same instance)

**Decision:** Hangfire uses the same SQL Server instance as the main application database.

**Reason:** Avoids adding a Redis or secondary DB dependency for v1. Hangfire's SQL Server storage is sufficient for the expected job volume (monitoring + cache refresh jobs, not high-throughput queuing). Revisit for v2 if job volume increases significantly.

---

## ADR-009 — No soft deletes anywhere

**Decision:** Entities are not soft-deleted. Records are either immutable (transactions, audit log) or have a status field (exceptions, rules).

**Reason:** Soft deletes complicate queries, indexing, and unique constraints. The audit log already provides full historical visibility. Rules are deactivated via `IsActive = false` rather than deleted.

---

## ADR-010 — CorrelationId propagated across all boundaries

**Decision:** A `CorrelationId` GUID is generated at ingestion, carried in ASB message headers, and written to every audit log entry.

**Reason:** Enables end-to-end trace of a transaction across HTTP, async messaging, and background jobs without requiring a distributed tracing platform in v1.
