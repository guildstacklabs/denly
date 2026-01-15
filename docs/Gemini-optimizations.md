# Gemini Code Assist: Optimization & Improvement Plan

This document outlines specific improvements, edge-case handling, and optimizations based on the review of `TESTING-strategy.md`, `ARCHITECTURE-data-model.md`, and `AGENTS.md`.

## 1. Architecture & Testability (Critical)

**Context:** The app currently lacks automated tests, and direct Supabase dependencies make unit testing difficult.

### A. Abstraction of Supabase Client
*   **Problem:** Services likely instantiate or access `Supabase.Client` directly, making it impossible to mock network calls.
*   **Solution:** Introduce an `ISupabaseClientProvider` or wrap specific table interactions.
*   **Implementation Detail:**
    *   Create `IDataService<T>` or specific repository interfaces (e.g., `IExpenseRepository`).
    *   Refactor `SupabaseExpenseService` to depend on these interfaces rather than the concrete `Supabase.Client`.
    *   **Benefit:** Enables the "Priority Test Targets" listed in the testing strategy (Expense calculations, Den selection).

### B. Centralized Error Handling & User Feedback
*   **Problem:** `AGENTS.md` suggests a `try/catch` pattern with `Console.WriteLine`. This is insufficient for production mobile apps.
*   **Solution:** Create a `IUserFeedbackService`.
*   **Implementation Detail:**
    *   Interface methods: `ShowError(string message)`, `ShowSuccess(string message)`.
    *   Implementation: Use `CommunityToolkit.Maui.Alerts.Toast` for non-intrusive feedback.
    *   Replace `Console.WriteLine` in catch blocks with `_feedbackService.ShowError("Unable to load expenses.")`.

## 2. UI/UX & Perceived Performance

**Context:** Mobile users expect instant feedback. Waiting for a server roundtrip for every interaction feels sluggish.

### A. Optimistic UI Updates
*   **Improvement:** When a user adds an Expense or Event *and is online*, update the UI list *immediately* before the API call completes.
*   **Edge Case:** If the API call fails, rollback the change and notify the user.
*   **Implementation:**
    *   In `Expenses.razor` (or ViewModel), add the item to the `ObservableCollection` / List immediately.
    *   `await _service.AddAsync(item);`
    *   `catch`: Remove item from list, show error.

### B. Skeleton Loading States
*   **Improvement:** Replace simple spinners with Skeleton Loaders (gray bars mimicking text) during data fetching.
*   **Why:** Reduces "layout shift" when data pops in.
*   **Target:** Expense lists and Dashboard summaries.

## 3. Edge Cases & Robustness

**Context:** Mobile devices have unstable connections, and the multi-den model introduces complex permission states.

### A. Network Connectivity Guardrails
*   **Edge Case:** User tries to save an expense while in an elevator (no signal).
*   **Current Risk:** App might hang, crash, or leave data in an inconsistent state.
*   **Optimization:**
    *   Inject `IConnectivity` (MAUI Essentials).
    *   **Strategy:** "Read-Only Offline".
    *   If `Connectivity.NetworkAccess != Internet`:
        *   Disable "Add/Edit" buttons or intercept clicks with a "Connect to internet to save" toast.
        *   Show a non-intrusive "Offline Mode" banner.
    *   **Why:** Avoids the complexity of offline sync/conflict resolution (Store & Forward) for the MVP phase.

### B. Role-Based UI Filtering (Observer Pattern)
*   **Edge Case:** An "Observer" (grandparent) tries to delete an event. RLS will block it, but the UI shouldn't even offer the option.
*   **Optimization:**
    *   Cache the current user's Role for the active Den upon load.
    *   Create a Blazor component `<AuthorizeRole Roles="Owner,Co-parent">` to wrap Edit/Delete buttons.
    *   **Benefit:** Prevents frustration by hiding actions that are guaranteed to fail.

### C. Den Switching "Zombie" State
*   **Edge Case:** User A is viewing Den 1. User B (Owner) deletes Den 1. User A pulls to refresh.
*   **Optimization:**
    *   Handle `PGRstException` (Postgrest) specifically for 404/406 errors.
    *   If the current Den is not found, automatically fallback to the next available Den or the "Create Den" screen.
    *   Clear "Last Used Den" from local storage if validation fails.

### D. App Lifecycle & Stale Data
*   **Edge Case:** User opens app, backgrounds it for 4 hours, then resumes. Auth token might be near expiry or data stale.
*   **Optimization:**
    *   Hook into `Window.Resumed` (MAUI Lifecycle).
    *   If elapsed time > 15 mins, trigger a "soft refresh" of the dashboard and validate the session.
    *   **Benefit:** Prevents users from acting on stale data or seeing "Auth Error" toasts immediately upon return.

## 4. Cost & Data Optimization

**Context:** `AGENTS.md` emphasizes cost transparency.

### A. Aggressive Caching for "Static" Data
*   **Target:** `GetBalancesAsync` and `GetDenMembersAsync`.
*   **Optimization:**
    *   These values rarely change within a single session.
    *   Implement a simple in-memory cache (Dictionary) in the Service with a short TTL (Time To Live) or invalidate only on specific actions (e.g., adding an expense invalidates the balance cache).

### B. Select Specific Columns
*   **Optimization:** Ensure Supabase queries use `.Select("id, name, amount")` rather than `*`.
*   **Why:** Reduces payload size, especially if `documents` or large JSON blobs are added to tables later.

### C. Client-Side Image Compression
*   **Target:** Receipt and Document uploads.
*   **Optimization:**
    *   Modern phone cameras take 5MB+ images.
    *   Resize/compress images to max 1024px or ~80% quality *before* upload.
    *   **Benefit:** Faster uploads on bad networks, significantly lower Storage costs, and faster downloads for the other co-parent.

---
*Generated by Gemini Code Assist for implementation by Claude Code.*