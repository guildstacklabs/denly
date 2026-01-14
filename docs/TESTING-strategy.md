# Testing Strategy (Initial)

This plan focuses on establishing a **small, reliable test baseline** that can grow incrementally.
The app has no automated tests today, so the priority is **fast feedback** and **service-level coverage**.

## 1) Create a Unit Test Project
**Recommendation:** Add `Denly.Tests` (xUnit) with the following packages:
- `xunit`
- `xunit.runner.visualstudio`
- `FluentAssertions` (optional, but improves readability)
- `NSubstitute` or `Moq` for mocking

**Why:**
- Allows us to validate service logic without device emulators.
- Establishes a CI-friendly test target for fast feedback.

## 2) Wrap Supabase Client for Testability
**Problem:** Supabase client calls are static/inline, hard to mock.

**Recommendation:**
- Introduce an `ISupabaseClient` abstraction or a thin wrapper
- Services depend on the abstraction, not the concrete client

**Why:**
- Enables full unit tests for services without hitting Supabase
- Keeps production behavior unchanged

## 3) Priority Test Targets (Short-Term)
Start with **deterministic, low-flake** tests:

1. **Expense calculations**
   - `GetBalancesAsync()` should compute fair-share correctly
   - Use a fake data set in memory

2. **Den selection behavior**
   - When den is missing, services return empty results or throw appropriately

3. **Authentication state behaviors**
   - `HasDenAsync()` should reflect den state
   - Session restore/persist logic (safe fallbacks)

4. **Receipt handling**
   - `SaveReceiptAsync()` should validate file paths and return a URL format
   - Use a mock storage service to avoid network calls

## 4) Component-Level Tests (Blazor)
**Recommendation:** Introduce `bUnit` for Blazor component testing.

**Targets:**
- Pages that load and render lists (expenses, schedules)
- Loading and empty-state views
- Basic form validation behavior

**Why:**
- Verifies UI logic without running MAUI
- Great for regression testing on UX states

## 5) Integration Tests (Longer-Term)
**Recommendation:** Consider a separate test suite for Supabase-backed integration tests.

**Options:**
- Use Supabase local dev stack (Docker) for predictable results
- Run a small set of nightly tests (not per-PR) to limit cost

**Why:**
- Validates RLS policies and actual query behavior
- Catches API regressions that unit tests can’t

## 6) CI Checkpoints
**Recommendation:**
- Add a `dotnet test` step to CI once the test project exists
- Keep tests fast (< 1–2 min) to maintain developer velocity

## 7) Test Data & Fixtures
**Recommendation:**
- Add a `TestDataBuilder` for commonly used entities
- Use fake clock (`IClock`) to make timestamps deterministic

---

## Suggested First Tests (Concrete Examples)
1. **Balances**
   - `GetBalancesAsync` with 2 members and 3 expenses
   - Assert correct owed/owes amounts

2. **Den Guardrails**
   - Ensure services return empty collections if no den is selected

3. **Auth Persistence**
   - `RestoreSessionAsync` does not throw if storage is missing

These three tests provide strong coverage for critical behavior without
any Supabase dependencies.
