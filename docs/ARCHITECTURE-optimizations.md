# Architecture Optimization Review

This document captures architectural optimization opportunities identified during a codebase sweep.
It is intentionally scoped to **low-risk, high-leverage** changes that improve maintainability,
performance, and security without destabilizing the app.

## 1) Replace Console Logging with Structured Logging (Security-First)
**Problem:** Services and pages log sensitive identifiers (user IDs, access tokens, invite codes),
stack traces, and potentially PII directly to the console. This is risky for a co-parenting app.

**Recommendation:**
- Replace `Console.WriteLine` with `ILogger<T>` in all Services and Components.
- Establish a logging policy:
  - Never log user IDs, den IDs, invite codes, access tokens, or raw PII.
  - Only log high-level context and error identifiers.
  - Use log levels (`Debug/Information/Warning/Error`) to keep production noise low.
- Use structured logs with stable event names (e.g., `Auth.SignIn.Started`, `Den.Invite.Failed`).
- Remove stack trace logging from UI pages; log only in services if needed.

**Targets:**
- `Services/SupabaseAuthService.cs`
- `Services/SupabaseDenService.cs`
- `Services/SupabaseExpenseService.cs`
- `Services/SupabaseScheduleService.cs`
- `Services/SupabaseStorageService.cs`
- `Services/SupabaseDocumentService.cs`
- `Components/Pages/Home.razor`
- `Components/Pages/CreateDen.razor`
- `Components/Pages/JoinDen.razor`
- `Components/Pages/Settings.razor`
- `Components/Pages/Calendar.razor`

**Why it helps:**
- Prevents leaking PII in shared logs
- Enables redaction and filtering with real logging pipelines

## 2) Centralize Auth/Den Guards in SupabaseServiceBase
**Problem:** Services re-check auth, den selection, and Supabase client state with slightly different
behavior, and some methods fetch den ID before initialization.

**Recommendation:**
- Add guard helpers to `Services/SupabaseServiceBase.cs`:
  - `Task EnsureInitializedAsync()` (already exists)
  - `string GetCurrentDenIdOrThrow()` for use in write operations
  - `string? TryGetCurrentDenId()` for read operations
  - `string GetAuthenticatedUserIdOrThrow()` to ensure auth before writes
  - `Supabase.Client GetClientOrThrow()` to eliminate `!` usage
- Normalize behavior:
  - Read methods return empty lists on missing den, write methods throw or return failure result.
  - Always call `EnsureInitializedAsync()` before accessing `DenService` or `SupabaseClient`.

**Targets:**
- `Services/SupabaseServiceBase.cs` (new helpers)
- `Services/SupabaseDocumentService.cs` (ensure init before den access)
- `Services/SupabaseExpenseService.cs`
- `Services/SupabaseScheduleService.cs`

**Why it helps:**
- Prevents inconsistent error paths
- Reduces duplicated logic and null checks
- Aligns with "stability over features"

## 3) Reduce Full-Table Fetches on Home Dashboard
**Problem:** Home page loads all expenses and a large slice of events to infer "new user" status,
which is expensive and increases latency.

**Recommendation:**
- Add light-weight, targeted service APIs:
  - `Task<bool> HasExpensesAsync()` in `IExpenseService`
  - `Task<bool> HasUpcomingEventsAsync()` or `HasEventsAsync()` in `IScheduleService`
  - `Task<bool> HasDocumentsAsync()` in `IDocumentService` or `GetRecentDocumentsAsync(1)`
- Update `Components/Pages/Home.razor` to use these lightweight checks.
- Load dashboard data in parallel using `Task.WhenAll` for upcoming events, balances, and recent docs.

**Targets:**
- `Components/Pages/Home.razor`
- `Services/IScheduleService.cs`, `Services/SupabaseScheduleService.cs`
- `Services/IExpenseService.cs`, `Services/SupabaseExpenseService.cs`
- `Services/IDocumentService.cs`, `Services/SupabaseDocumentService.cs`

**Why it helps:**
- Reduces database calls and payload sizes
- Faster, more responsive dashboard load

## 4) Batch Updates for Settlements
**Problem:** Settlements update each expense row in a loop, resulting in multiple round-trips.

**Recommendation:**
- Replace per-row updates with a single bulk update:
  - Add a filtered update call on `Expense` where `den_id` and `settled_at IS NULL`
  - Or add a Postgres RPC function for `settle_expenses(den_id, settled_at)`
- Ensure RLS policies allow updates for the current user within their den.

**Targets:**
- `Services/SupabaseExpenseService.cs`
- `docs/DATABASE.md` (document any RPC or view)

**Why it helps:**
- Fewer network calls, less battery usage on mobile
- Eliminates partial updates if the loop fails mid-way

## 5) Move Document Search and Counts Server-Side
**Problem:** Search and folder counts currently download all documents, then filter in memory.

**Recommendation:**
- Add server-side filters for search:
  - Use `ilike` or full text search in Postgres
  - Provide `SearchDocumentsAsync` that queries directly on the server
- Provide a `GetFolderCountsAsync` implementation backed by SQL (view or RPC).
- Document RLS implications in `docs/DATABASE.md`.

**Targets:**
- `Services/SupabaseDocumentService.cs`
- `docs/DATABASE.md`

**Why it helps:**
- Scales better as document volume grows
- Reduces memory use in the WebView

## 6) Improve Storage Upload Memory Usage
**Problem:** `SupabaseStorageService` copies file streams into memory before uploading.
This is risky for large files on mobile devices.

**Recommendation:**
- If Supabase SDK supports streaming upload, switch to streaming.
- If not, at least add a size guard and user-facing error for oversized uploads.

**Targets:**
- `Services/SupabaseStorageService.cs`

**Why it helps:**
- Avoids large memory spikes and potential crashes

## 7) Normalize Error Handling and Loading States
**Problem:** Async service methods are inconsistent about try/catch and user feedback.

**Recommendation:**
- Enforce a consistent try/catch/finally pattern for async operations.
- Service methods should return safe defaults on read failures and throw only for writes.
- UI components should always show loading state while awaiting.

**Targets:**
- `Services/*`
- `Components/Pages/*.razor`

**Why it helps:**
- Stability: app should never crash or appear frozen
- Clearer user feedback paths

## 8) Edge Cases to Address (Behavioral + Data Integrity)
**Problem:** There are several subtle edge cases that can cause incorrect behavior, silent failures,
or data inconsistencies, especially around auth, time zones, and storage.

**Recommendations:**
- **Auth/session restore failure:** `SupabaseAuthService.RestoreSessionAsync` swallows exceptions.
  Add telemetry and a user-friendly reauth path when session restore fails or is invalid.
- **Den selection race:** multiple service calls can run before `DenService.InitializeAsync` completes.
  Ensure all service entry points call `EnsureInitializedAsync` before any den/user access.
- **Cache staleness:** `SupabaseDenService` caches members/profiles for 5 minutes.
  Invalidate cache on `DenChanged`, member add/remove, and profile update events.
- **Invite code leakage:** invite validation is readable by anyone; ensure `ValidateInviteCodeAsync`
  only returns minimal den info and does not log codes or den IDs.
- **Timezone/DST correctness:**
  - `GetEventsByDateAsync` assumes local dates; verify behavior across DST shifts and all-day events.
  - Avoid double conversion and ensure `DateTimeKind` is correct on inputs from UI.
- **Settlement consistency:** `CreateSettlementAsync` marks all unsettled expenses in a loop.
  This can fail mid-way or miss concurrent inserts; use a single bulk update or transaction/RPC.
- **Storage URL parsing:** `SupabaseStorageService.DeleteAsync` assumes public bucket URL shape.
  Guard for signed URLs, custom domains, or malformed URLs.
- **Null/empty user IDs:** Some tables allow nullable `paid_by`, `created_by`, etc.
  Ensure services handle nulls gracefully when computing balances or display names.

**Targets:**
- `Services/SupabaseAuthService.cs`
- `Services/SupabaseDenService.cs`
- `Services/SupabaseScheduleService.cs`
- `Services/SupabaseExpenseService.cs`
- `Services/SupabaseStorageService.cs`
- `docs/DATABASE.md` (RLS notes and RPC/transaction expectations)

**Why it helps:**
- Reduces silent failure modes
- Improves correctness under concurrency and time zone edge cases
