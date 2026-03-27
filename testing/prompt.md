Prompt 1: Domain & Validation (Unit Tests)
"Write xUnit tests using FluentAssertions for the Domain and Application validation layers of the Financial Reconciliation Engine.

    Domain Tests: Verify that Transaction, ReconciliationRecord, ExceptionRecord, and AuditLog entities strictly initialize timestamps in UTC. Ensure that domain events like TransactionIngested, TransactionMatched, and ExceptionRaised are correctly added to the entities' internal event collections.
    Validation Tests: Write tests for the FluentValidation behavior on the IngestTransactionCommand. Assert that a 400 Bad Request is triggered if: Amount is 0 or negative, Currency is not a valid 3-letter ISO 4217 code, Source or ExternalId exceed 255 characters, or the TransactionDate is in the future."

Prompt 2: Idempotent Ingestion & CQRS (Integration Tests)
"Write integration tests for the IngestTransactionCommand handler using a test database (e.g., Testcontainers or SQLite in-memory).

    First Ingestion: Simulate ingesting a valid new transaction. Assert that it writes to the DB with a Pending status, adds an entry to the AuditLog, returns a 201 Created, and publishes a TransactionIngested event to the mocked Azure Service Bus publisher.
    Idempotency Check: Re-send the exact same command (same Source and ExternalId). Assert that the handler catches the unique constraint conflict and returns a 200 OK with the existing record, without creating a duplicate, writing a new audit log, or publishing a new ASB event.
    Verify that every operation logged to the AuditLog during this process carries the original CorrelationId."

Prompt 3: The Matching Pipeline Logic (Unit Tests)
"Write unit tests for the async matching pipeline using a mocked ITransactionRepository. The pipeline must short-circuit as soon as a match is found.

    Exact Match: Test that a match succeeds when Amount, Currency, Date, and Reference (trimmed, case-insensitive) match perfectly.
    Fuzzy Match: Test that if Exact fails, Jaro-Winkler similarity correctly matches a Reference/Description >= the 0.92 default threshold, provided the date is within ±1 day.
    Fuzzy Mismatch Tie: Create a test where two candidates score identically on the fuzzy match. Assert that the pipeline stops and escalates this as an ExceptionRecord with the Mismatch category.
    Rule-Based Match: Test that rules are evaluated in ascending Priority order.
    Unmatched: If all three steps fail, assert that an ExceptionRecord is created with the Unmatched category and a PendingReview status."

Prompt 4: Infrastructure, Security & Audit (Integration Tests)
"Write integration tests focusing on the Infrastructure layer's security and immutability constraints.

    Encryption: Mock the IEncryptionService (which uses AES-256 and Key Vault). Write a transaction and an exception record to the DbContext. Query the raw database connection directly to assert that AccountId, Description, and Notes are physically stored as ciphertexts and not plain text.
    Append-Only Audit Log: Attempt to issue an Entity Framework Update or Remove command on an AuditLog record. Assert that the context throws an exception or rejects the operation, verifying the ADR-003 append-only constraint."

Prompt 5: API, Middleware & Observability (E2E Tests)
"Write End-to-End API tests using WebApplicationFactory.

    Global Exception Handling: Force an unhandled infrastructure exception (e.g., mock a DB failure). Assert that the API returns a 500 Internal Server Error formatted strictly as an RFC 9110 Problem Details JSON. Assert that the response body does not contain stack traces, SQL errors, or any PII.
    Correlation ID: Send an HTTP GET to /transactions. Assert that the response contains an X-Correlation-Id header with a valid GUID.
    Role-Based Auth: Attempt to access the /hangfire dashboard endpoint using a JWT token minted with the 'Operator' role. Assert that it returns a 403 Forbidden, as only 'Admin' roles should have access.