# Financial Reconciliation Engine — Matching Pipeline

## Overview

The matching pipeline is the core of the reconciliation engine. It runs asynchronously, triggered by Azure Service Bus events after a transaction is ingested. It attempts to find a counterpart transaction from another source using a sequence of progressively looser matching strategies.

---

## Pipeline Trigger

When a transaction is successfully ingested, an event is published to Azure Service Bus:

```
Topic: reconciliation.transactions
Event: TransactionIngested
Payload: { TransactionId, Source, CorrelationId }
```

A Hangfire background job or ASB consumer picks this up and initiates the pipeline.

---

## Pipeline Steps

Steps run in order. The pipeline short-circuits as soon as a match is found.

```
Transaction (Pending)
    │
    ▼
┌─────────────────────────┐
│  Step 1: Exact Match    │ ── Match found? ──► Reconcile (method: Exact)
└─────────────────────────┘
    │ No match
    ▼
┌─────────────────────────┐
│  Step 2: Fuzzy Match    │ ── Match found? ──► Reconcile (method: Fuzzy)
│  (Jaro-Winkler)         │
└─────────────────────────┘
    │ No match
    ▼
┌─────────────────────────┐
│  Step 3: Rule-Based     │ ── Match found? ──► Reconcile (method: RuleBased)
│  (configurable rules)   │
└─────────────────────────┘
    │ No match
    ▼
┌─────────────────────────┐
│  Raise Exception        │ ── Category: Unmatched
└─────────────────────────┘
```

---

## Step 1 — Exact Match

Compares candidate transactions from other sources against the incoming transaction on strict equality.

**Match Criteria (all must match):**

| Field | Comparison |
|---|---|
| `Amount` | Exact decimal equality |
| `Currency` | Exact string match |
| `TransactionDate` | Exact date equality |
| `Reference` | Exact string match (trimmed, case-insensitive) |

**Candidate Pool:** Transactions with `Status = Pending` from any source other than the incoming transaction's source.

**On match:** Create `ReconciliationRecord` with `MatchMethod = Exact`, `ConfidenceScore = null`. Update both transactions to `Status = Matched`.

---

## Step 2 — Fuzzy Match (Jaro-Winkler)

Applies when exact match finds nothing. Loosens the reference/description comparison using Jaro-Winkler similarity scoring.

**Match Criteria:**

| Field | Comparison |
|---|---|
| `Amount` | Exact decimal equality |
| `Currency` | Exact string match |
| `TransactionDate` | Within ±1 calendar day |
| `Reference` or `Description` | Jaro-Winkler score ≥ configurable threshold (default: 0.92) |

**Threshold** is configurable per environment via app settings (`Matching:FuzzyThreshold`).

**On match:** Create `ReconciliationRecord` with `MatchMethod = Fuzzy`, `ConfidenceScore = <jaro-winkler score>`.

**Multiple candidates:** If more than one candidate meets the threshold, take the highest-scoring one. If two candidates score identically, escalate to an exception (`Category = Mismatch`).

---

## Step 3 — Rule-Based Match

Applies configurable rules evaluated in priority order. Rules are loaded from the `MatchingRules` table at pipeline startup (cached with a configurable TTL).

**Rule evaluation:**

- Rules are evaluated in ascending `Priority` order (lower number = evaluated first)
- First rule that produces a match wins
- Each rule defines which fields to compare and any tolerance parameters

**Example rule config:**
```json
{
  "fields": ["reference"],
  "amountTolerancePercent": 1.5,
  "dateTolerance": 2
}
```

This rule matches if `reference` is identical and `amount` is within 1.5% and `transactionDate` is within ±2 days.

**On match:** Create `ReconciliationRecord` with `MatchMethod = RuleBased`, `RuleId = <matched rule id>`, `ConfidenceScore = null`.

---

## Exception Handling

If all three steps fail to find a match, an `ExceptionRecord` is created.

**Categories:**

| Category | Trigger |
|---|---|
| `Unmatched` | No candidate found across all steps |
| `Mismatch` | Multiple candidates found with identical fuzzy scores |
| `Duplicate` | Incoming transaction is near-identical to an already-matched transaction |

**On exception:** Transaction `Status` is updated to `Exception`. `ExceptionRecord` created with `Status = PendingReview`.

---

## Idempotency in the Pipeline

If the pipeline is re-triggered for a transaction that is already `Matched` or `Exception`, it exits immediately without reprocessing. This prevents duplicate reconciliation records from retry scenarios.

---

## Observability

Every pipeline execution writes to the `AuditLog`:

- `TransactionMatchingStarted` — pipeline initiated
- `ExactMatchAttempted` — result: matched / no match
- `FuzzyMatchAttempted` — result + confidence score
- `RuleMatchAttempted` — result + rule id
- `ReconciliationCreated` — match confirmed
- `ExceptionRaised` — no match found

All audit entries carry the same `CorrelationId` as the originating ingestion event.

---

## Configuration

```json
{
  "Matching": {
    "FuzzyThreshold": 0.92,
    "FuzzyDateToleranceDays": 1,
    "RuleCacheTtlMinutes": 10,
    "MaxCandidates": 100
  }
}
```
