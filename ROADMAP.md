# Technical Roadmap: Denly

## Phase 1: Stabilization & Polish (Current Focus)

### 1.1 UI Responsiveness & Feedback
- [ ] **Loading States**: Implement `_isLoading` flags in all pages (`Home`, `Expenses`, `Calendar`, `Vault`).
    - *Action*: Show a spinner or skeleton loader while `OnInitializedAsync` fetches data.
- [ ] **Error Handling UI**: Replace console logs with user-visible feedback.
    - *Action*: Create a global `ToastService` or alert mechanism to inform users when operations fail (e.g., "Failed to load expenses").
- [ ] **Empty States**: Ensure all lists have friendly "empty state" messages and call-to-actions (already partially done, need review).

### 1.2 Code Quality & Reliability
- [ ] **Service Layer Refactoring**:
    - Ensure all Supabase calls are wrapped in `try/catch`.
    - Centralize error logging.
    - Remove direct `Console.WriteLine` in favor of a logging abstraction (even if it just wraps Console for now).
- [ ] **Data Integrity**:
    - Verify `SaveExpenseAsync` and other mutation methods handle race conditions or network failures gracefully.

### 1.3 Security Audit
- [ ] Review `AGENTS.md` compliance.
- [ ] Verify Row Level Security (RLS) policies on Supabase match the application logic (Den-based isolation).

---

## Phase 2: Architecture & Reliability (Pre-Launch)

### 2.1 State Management Optimization
- [ ] **Caching**: Implement simple memory caching in Services to avoid re-fetching data on every navigation.
    - *Benefit*: Instant page loads for frequently visited screens.
- [ ] **Singleton State**: Ensure `CurrentUser` and `CurrentDen` are reliably available without constant refetching.

### 2.2 Offline Preparation
- [ ] **Local Storage**: Investigate `MonkeyCache` or SQLite for storing data locally.
- [ ] **Sync Logic**: Design a strategy for syncing local changes back to Supabase (Post-MVP, but design now).

---

## Phase 3: Feature Enhancements (Post-MVP)

### 3.1 Advanced Expense Features
- [ ] Timeline View for expenses.
- [ ] Recurring expenses.
- [ ] Category management.

### 3.2 Notification System
- [ ] Push notifications for new expenses, schedule changes, or messages.

### 3.3 CI/CD & Testing
- [ ] Set up GitHub Actions for building .NET MAUI.
- [ ] Add `xUnit` project for core logic testing.
- [ ] Add `bUnit` for component testing.
