# Architecture Optimization Review

This document captures architectural optimization opportunities identified during a codebase sweep.
It is intentionally scoped to **low-risk, high-leverage** changes that improve maintainability,
performance, and security without destabilizing the app.

## 1) Replace Console Logging with Structured Logging (Remaining Item)
**Problem:** Services currently log sensitive IDs and access tokens directly to the console,
which is a security and privacy risk.

**Recommendation:**
- Replace `Console.WriteLine` with `ILogger<T>`.
- Add a logging policy:
  - Never log user IDs, access tokens, or raw PII
  - Only log high-level context and error identifiers
  - Use log levels (`Debug/Information/Warning/Error`) to reduce noise

**Why it helps:**
- Prevents leaking PII in shared logs
- Enables redaction and filtering with real logging pipelines

