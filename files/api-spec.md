# Financial Reconciliation Engine — API Spec

## Base URL

```
/api/v1
```

All endpoints return `application/json`. All timestamps are ISO 8601 UTC.

---

## Authentication

Bearer token (JWT) required on all endpoints via `Authorization: Bearer <token>` header.

---

## Endpoints

---

### Transactions

#### `POST /transactions/ingest`

Ingest a single transaction. Idempotent — re-posting the same `(source, externalId)` returns 200 with the existing record, no duplicate created.

**Request Body**
```json
{
  "source": "BankFeedA",
  "externalId": "TXN-00123",
  "amount": 1500.00,
  "currency": "ZAR",
  "transactionDate": "2024-11-01",
  "description": "Invoice payment",
  "reference": "INV-9981",
  "accountId": "ACC-4421"
}
```

**Responses**

| Status | Meaning |
|---|---|
| `201 Created` | New transaction ingested |
| `200 OK` | Duplicate detected; existing record returned |
| `400 Bad Request` | Validation failure |
| `401 Unauthorized` | Missing or invalid token |

**Response Body (201 / 200)**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "source": "BankFeedA",
  "externalId": "TXN-00123",
  "status": "Pending",
  "ingestedAt": "2024-11-01T08:00:00Z"
}
```

---

#### `POST /transactions/ingest/batch`

Ingest multiple transactions in a single call. Each record is processed idempotently. Partial success is supported — failed items are returned with reasons.

**Request Body**
```json
{
  "transactions": [ /* array of ingest request objects */ ]
}
```

**Response Body**
```json
{
  "succeeded": 47,
  "skipped": 3,
  "failed": 1,
  "failures": [
    {
      "externalId": "TXN-00099",
      "reason": "Invalid currency code"
    }
  ]
}
```

---

#### `GET /transactions/{id}`

Retrieve a single transaction by internal ID.

**Response Body**
```json
{
  "id": "3fa85f64-...",
  "source": "BankFeedA",
  "externalId": "TXN-00123",
  "amount": 1500.00,
  "currency": "ZAR",
  "transactionDate": "2024-11-01",
  "reference": "INV-9981",
  "status": "Matched",
  "ingestedAt": "2024-11-01T08:00:00Z"
}
```

---

#### `GET /transactions`

List transactions with optional filters.

**Query Parameters**

| Param | Type | Description |
|---|---|---|
| `source` | string | Filter by source system |
| `status` | string | `Pending`, `Matched`, `Exception` |
| `from` | date | Transaction date range start |
| `to` | date | Transaction date range end |
| `page` | int | Default: 1 |
| `pageSize` | int | Default: 50, max: 200 |

---

### Reconciliation

#### `GET /reconciliations/{id}`

Get a reconciliation record including matched transactions.

**Response Body**
```json
{
  "id": "abc123...",
  "matchMethod": "Fuzzy",
  "confidenceScore": 0.9312,
  "matchedAt": "2024-11-01T08:05:00Z",
  "transactions": [
    { "id": "...", "source": "BankFeedA", "externalId": "TXN-00123" },
    { "id": "...", "source": "ERP", "externalId": "ERP-88721" }
  ]
}
```

---

#### `GET /reconciliations`

List reconciliation records.

**Query Parameters:** `matchMethod`, `from`, `to`, `page`, `pageSize`

---

### Exceptions

#### `GET /exceptions`

List exception records for the review queue.

**Query Parameters**

| Param | Type | Description |
|---|---|---|
| `status` | string | `PendingReview`, `UnderReview`, `Resolved`, `Dismissed` |
| `category` | string | `Mismatch`, `Duplicate`, `Unmatched` |
| `assignedTo` | string | Filter by reviewer |
| `page` | int | Default: 1 |
| `pageSize` | int | Default: 50 |

---

#### `GET /exceptions/{id}`

Get a single exception with full detail.

---

#### `PATCH /exceptions/{id}/assign`

Assign an exception to a reviewer.

**Request Body**
```json
{
  "assignedTo": "reviewer@company.com"
}
```

---

#### `PATCH /exceptions/{id}/resolve`

Mark an exception as resolved.

**Request Body**
```json
{
  "resolution": "Resolved",
  "notes": "Confirmed legitimate duplicate from legacy feed migration"
}
```

`resolution` values: `Resolved`, `Dismissed`

---

#### `PATCH /exceptions/{id}/categorise`

Update the category of an exception.

**Request Body**
```json
{
  "category": "Duplicate"
}
```

---

### Audit

#### `GET /audit`

Query the audit log.

**Query Parameters**

| Param | Type | Description |
|---|---|---|
| `entityType` | string | `Transaction`, `ExceptionRecord`, etc. |
| `entityId` | GUID | Filter by specific entity |
| `correlationId` | GUID | Trace across async operations |
| `from` | datetime | UTC range start |
| `to` | datetime | UTC range end |
| `page` | int | Default: 1 |
| `pageSize` | int | Default: 100 |

**Response Body**
```json
{
  "items": [
    {
      "id": 10021,
      "entityType": "Transaction",
      "entityId": "3fa85f64-...",
      "action": "Ingested",
      "performedBy": "IngestionService",
      "performedAt": "2024-11-01T08:00:00Z",
      "correlationId": "e9d1c3a2-..."
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 100
}
```

---

### Matching Rules

#### `GET /rules`

List all matching rules.

#### `POST /rules`

Create a new rule-based matching rule.

**Request Body**
```json
{
  "id": "RULE_AMOUNT_REF_TOLERANCE",
  "description": "Match on reference and amount within 1% tolerance",
  "priority": 10,
  "configJson": {
    "fields": ["reference"],
    "amountTolerancePercent": 1.0
  }
}
```

#### `PATCH /rules/{id}`

Update a rule's config, priority, or active status.

#### `DELETE /rules/{id}`

Deactivate a rule (sets `isActive = false`; does not delete).

---

## Standard Error Response

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation failed",
  "status": 400,
  "errors": {
    "amount": ["Amount must be greater than zero"],
    "currency": ["Currency must be a valid ISO 4217 code"]
  },
  "traceId": "00-abc123-def456-00"
}
```
