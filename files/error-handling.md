# Financial Reconciliation Engine — Error Handling

## Principles

- All errors are caught at the API boundary via global exception middleware
- Validation errors are returned before any business logic executes
- Infrastructure errors (DB, ASB, Key Vault) are logged and surfaced as 500s — never leak internals
- Async pipeline errors (matching failures) are caught per-message; failed messages go to dead-letter queue

---

## HTTP Error Response Format

All error responses follow RFC 9110 Problem Details:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more fields failed validation",
  "errors": {
    "amount": ["Amount must be greater than zero"],
    "currency": ["Currency must be a valid ISO 4217 code"]
  },
  "traceId": "00-abc123-def456-00"
}
```

---

## Status Codes

| Status | When Used |
|---|---|
| `200 OK` | Idempotent re-ingest of existing record |
| `201 Created` | New resource created |
| `400 Bad Request` | Validation failure |
| `401 Unauthorized` | Missing or invalid JWT |
| `403 Forbidden` | Valid JWT but insufficient role |
| `404 Not Found` | Resource does not exist |
| `409 Conflict` | Concurrent write conflict (rare) |
| `500 Internal Server Error` | Unhandled infrastructure failure |

---

## Validation

FluentValidation runs before MediatR handlers via a pipeline behaviour. Validation errors are collected and returned as a `400` before any DB or ASB calls are made.

**Key validation rules:**

- `Amount` > 0
- `Currency` must be a valid ISO 4217 3-letter code
- `Source` and `ExternalId` are required, max 255 chars
- `TransactionDate` must not be in the future

---

## Global Exception Middleware

Catches unhandled exceptions and:

1. Logs the full exception with `CorrelationId` and stack trace (structured log, never exposed to client)
2. Returns a sanitised `500` Problem Details response
3. Never exposes stack traces, SQL errors, or internal service names to the caller

---

## Async Pipeline Error Handling

### Azure Service Bus Consumer

- Each message is processed in a try/catch
- On exception: message is abandoned (triggers ASB retry with backoff)
- After max retries (3): message is dead-lettered
- Dead-letter queue monitored by `DeadLetterMonitorJob` (Hangfire)

### Matching Step Failures

- If a matching step throws (e.g. Key Vault timeout during decryption), the entire pipeline for that transaction is abandoned
- The transaction remains in `Pending` status
- The message is retried by ASB
- After dead-lettering, an alert is raised for manual investigation

---

## Exception Categories (Domain)

These are not HTTP errors — they are reconciliation exception types:

| Category | Meaning |
|---|---|
| `Unmatched` | No matching counterpart found after all steps |
| `Mismatch` | Multiple near-equal candidates found; ambiguous |
| `Duplicate` | Incoming transaction closely resembles an already-matched record |

---

## Logging Strategy

Structured logging via Serilog. Logs are written to:
- Console (local dev)
- Azure Application Insights (production)

**Log levels:**

| Event | Level |
|---|---|
| Transaction ingested | `Information` |
| Match found | `Information` |
| Exception raised | `Warning` |
| Dead-letter detected | `Error` |
| Unhandled exception | `Critical` |

**PII masking:** Fields like `AccountId`, `Description`, and `Reference` are masked in logs. Never log decrypted PII values.
