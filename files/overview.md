# Financial Reconciliation Engine — Overview

## Project Summary

A backend system for reconciling financial transaction records across multiple sources. It ingests transactions from disparate financial systems, runs them through a multi-step matching pipeline, flags unmatched or ambiguous records as exceptions, and maintains a full audit trail for compliance.

---

## Goals

- Accurately match transactions across multiple financial data sources
- Prevent duplicate ingestion of records
- Provide traceable, auditable reconciliation history
- Support manual review of exceptions by operations teams
- Comply with POPIA (Protection of Personal Information Act) requirements

---

## Tech Stack

| Concern | Technology |
|---|---|
| Runtime | .NET 8 / C# |
| Database | SQL Server |
| Messaging | Azure Service Bus |
| Mediator / CQRS | MediatR |
| Background Jobs | Hangfire |
| Secret Management | Azure Key Vault |
| String Matching | Jaro-Winkler (fuzzy) |

---

## Core Principles

- **Idempotent ingestion** — records are keyed on `(Source, ExternalId)`; re-ingesting the same record is a no-op
- **Append-only audit log** — no updates or deletes on audit records; full history preserved
- **POPIA-conscious** — minimal PII stored; sensitive fields encrypted at rest; keys managed via Azure Key Vault
- **Exception-first** — unmatched records surface immediately for human review rather than silently failing
- **Async processing** — ingestion and matching decoupled via Azure Service Bus

---

## High-Level Data Flow

```
External Source
    │
    ▼
[Ingestion API] ── idempotency check ──► skip if duplicate
    │
    ▼
[Azure Service Bus] (TransactionIngested event)
    │
    ▼
[Matching Pipeline Consumer]
    │
    ├── Step 1: Exact Match
    ├── Step 2: Fuzzy Match (Jaro-Winkler)
    └── Step 3: Rule-Based Match (configurable)
    │
    ├── MATCHED ──► ReconciliationRecord (status: Matched)
    └── UNMATCHED ──► ExceptionRecord (status: Pending Review)
    │
    ▼
[Audit Log] ── append-only, every state transition logged
    │
    ▼
[Exception Workflow] ── manual review, categorise, resolve
```

---

## Out of Scope (v1)

- UI / frontend
- Real-time streaming ingestion
- Rule-based matching pipeline
- Multi-currency conversion
- External reporting exports
