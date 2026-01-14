# Architecture Optimization Review

This document captures architectural optimization opportunities identified during a codebase sweep.
It is intentionally scoped to **low-risk, high-leverage** changes that improve maintainability,
performance, and security without destabilizing the app.

## 1) Normalize Service Initialization & Error Handling
**Problem:** Every service maintains its own initialization pattern and error-handling flow,
which leads to inconsistent retries, logging, and state checks.

**Recommendation:**
- Create a shared `SupabaseServiceBase` that encapsulates:
  - `EnsureInitializedAsync()` (auth + den restore)
  - Consistent `try/catch/finally` patterns
  - Safe access to `Supabase.Client` and current user
- Expose standardized error outputs (`Result<T>` or an app-level `ServiceResult`), so UI logic
  can present user-friendly feedback without parsing logs.

**Why it helps:**
- Reduces copy-paste code and divergent error handling
- Makes it easier to add instrumentation (e.g., `ILogger`) later
- Simplifies testing by allowing base behaviors to be mocked

## 2) Replace Console Logging with Structured Logging
**Problem:** Services currently log sensitive IDs and access tokens directly to the console,
which is a security and privacy risk.

**Status:** 🔄 In progress. Migrated schedule and expense services to `ILogger<T>` with PII-safe messages.

**Recommendation:**
- Replace `Console.WriteLine` with `ILogger<T>`.
- Add a logging policy:
  - Never log user IDs, access tokens, or raw PII
  - Only log high-level context and error identifiers
  - Use log levels (`Debug/Information/Warning/Error`) to reduce noise

**Why it helps:**
- Prevents leaking PII in shared logs
- Enables redaction and filtering with real logging pipelines

## 3) Centralize Supabase Configuration
**Problem:** Supabase URL and anonymous key are hard-coded in the auth service.

**Status:** ✅ Implemented via `DenlyOptions` and `appsettings.json` configuration binding.

**Recommendation:**
- Move Supabase config into app configuration (e.g., `appsettings.json` + secrets)
- Inject configuration via `IOptions<DenlyOptions>`

**Why it helps:**
- Prevents accidental key exposure
- Simplifies environment-specific configuration (dev/stage/prod)

## 4) Add Lightweight Caching for Profiles & Den Members
**Problem:** The app frequently re-fetches profile and member data within a single session.

**Recommendation:**
- Introduce an in-memory cache (per session) for:
  - `Profile` lookups by user ID
  - Den members list
- Use a simple TTL-based cache or an invalidation call when den context changes.

**Why it helps:**
- Reduces redundant API calls
- Improves UI responsiveness on slow networks

## 5) Introduce `IClock` for Time Handling
**Problem:** Services directly call `DateTime.UtcNow`, which makes tests brittle.

**Recommendation:**
- Inject an `IClock` abstraction:
  ```csharp
  public interface IClock { DateTime UtcNow { get; } }
  ```

**Why it helps:**
- Deterministic tests for time-sensitive features (settlements, created timestamps)
- Easier to simulate time-based edge cases

## 6) Adopt Cancellation Tokens for I/O
**Problem:** Long-running calls to Supabase and storage cannot be cancelled.

**Recommendation:**
- Add optional `CancellationToken` parameters to service methods
- Pass tokens from UI interactions where user navigation can cancel the operation

**Why it helps:**
- Prevents abandoned tasks from continuing in the background
- Improves app responsiveness

## 7) Isolate Storage Operations
**Problem:** Receipt storage logic lives inside `SupabaseExpenseService`, mixing data and storage concerns.

**Recommendation:**
- Extract storage into an `IReceiptStorageService`.
- Keep expense service focused on expense CRUD and orchestration.

**Why it helps:**
- Simplifies testing and reuse
- Makes it easier to swap storage providers later

## 8) Consistent Loading-State Handling
**Problem:** Loading state patterns differ across pages and services.

**Recommendation:**
- Use a shared loading helper in components or a service wrapper that exposes
  a `LoadingState` (e.g., `IsBusy`, `ErrorMessage`).

**Why it helps:**
- Avoids UI freezes
- Ensures async workflows always have visible feedback
