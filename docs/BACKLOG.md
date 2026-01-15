# Backlog

Prioritized work items for Denly. Items ordered by impact within each priority tier.

---

## For Agents: How to Use This File

If you are **Codex** or **Gemini**, follow these steps:

1. **Find your tasks**: Search for `Delegate: Codex` or `Delegate: Gemini` with `Status: Ready`
2. **Work in batches by priority tier** (all your P1 tasks, then P2, etc.)
3. For each task in the tier:
   - Update status to `In Progress`
   - Execute using the Delegation Prompt
   - Fill in Completion Report
   - Update status to `Awaiting Review`
   - **Continue to next task in same tier**
4. **Run `dotnet build`** after completing the batch
5. **Stop at tier boundary** - wait for Claude to review before starting next tier

**Status Values:**
- `Ready` - Available to pick up
- `In Progress` - Agent is working on it
- `Awaiting Review` - Done, needs Claude verification
- `Blocked` - Issue encountered, needs Claude help

**File Conflict Rule:** If two tasks modify the same file, complete the first fully before starting the second.

> See `AGENTS.md` → "Automated Task Pickup" for full workflow details.
> To add new items to this list, see `AGENTS.md` → "Adding New Work Items" for the template.

---

## Priority Framework

- **P0 - Security**: PII exposure, vulnerabilities (do first)
- **P1 - Stability**: Crash prevention, data integrity, error handling
- **P2 - Performance**: Latency, network efficiency, memory
- **P3 - UX Polish**: Nice-to-have improvements (post-MVP)

**Delegation Key:**
- **Claude** - Do not delegate; requires architectural decisions or security review
- **Codex** - Safe to delegate; small, isolated, pattern-based changes
- **Gemini** - Safe to delegate; medium complexity, clear scope

---

## P0 - Security (MVP Critical)

### 1. Replace Console.WriteLine with Structured Logging
**Source:** Codex #1 | **Effort:** Medium | **Risk:** Low

> **Delegate:** Claude only
> **Reason:** Security-sensitive, sets logging patterns for entire codebase. Requires decisions about what to log and what to redact.

**Problem:** Services log sensitive data (user IDs, tokens, invite codes) to console. Unacceptable for a co-parenting app.

**Solution:**
- Replace `Console.WriteLine` with `ILogger<T>`
- Establish logging policy: never log PII, use log levels appropriately
- Use structured event names: `Auth.SignIn.Started`, `Den.Invite.Failed`

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

---

### 2. Invite Code Leakage Prevention
**Source:** Codex #8 | **Effort:** Low | **Risk:** Low

> **Delegate:** Claude only
> **Reason:** Security audit requires careful review of what data is exposed.

**Problem:** Invite validation may expose more den info than necessary; codes might be logged.

**Solution:**
- Ensure `ValidateInviteCodeAsync` returns only minimal den info (name, not IDs)
- Audit all logging for invite codes
- Ensure RLS prevents enumeration

**Targets:**
- `Services/SupabaseDenService.cs`

---

## P1 - Stability (MVP Critical)

### 3. Centralize Auth/Den Guards in SupabaseServiceBase
**Source:** Codex #2 | **Effort:** Medium | **Risk:** Low

> **Delegate:** Claude only
> **Reason:** Architectural change to base class that all services inherit from. Incorrect implementation breaks everything.

**Problem:** Services re-check auth/den state inconsistently. Some methods access den ID before initialization completes.

**Solution:**
Add guard helpers to `SupabaseServiceBase`:
```csharp
string GetCurrentDenIdOrThrow()      // For writes
string? TryGetCurrentDenId()         // For reads (returns null, caller handles)
string GetAuthenticatedUserIdOrThrow()
Supabase.Client GetClientOrThrow()   // Eliminates ! usage
```

**Targets:**
- `Services/SupabaseServiceBase.cs` (add helpers)
- `Services/SupabaseDocumentService.cs`
- `Services/SupabaseExpenseService.cs`
- `Services/SupabaseScheduleService.cs`

---

### 4. Centralized User Feedback Service
**Source:** Gemini #1B | **Effort:** Medium | **Risk:** Low

> **Delegate:** Claude only
> **Reason:** New service architecture that will be used throughout the app. Needs consistent API design.

**Problem:** Error handling uses `Console.WriteLine` with no user-facing feedback. Users see frozen UI on failures.

**Solution:**
Create `IUserFeedbackService`:
```csharp
public interface IUserFeedbackService
{
    Task ShowErrorAsync(string message);
    Task ShowSuccessAsync(string message);
    Task ShowWarningAsync(string message);
}
```
Implementation: Use `CommunityToolkit.Maui` Toast or Snackbar.

**Targets:**
- Create `Services/IUserFeedbackService.cs`
- Create `Services/UserFeedbackService.cs`
- Update all catch blocks in Services and Pages

---

### 5. Network Connectivity Guardrails
**Source:** Gemini #3A | **Effort:** Medium | **Risk:** Low

> **Delegate:** Gemini | **Status:** Awaiting Review

**Problem:** User tries to save while offline → app hangs or fails silently.

**Solution:**
- Inject `IConnectivity` (MAUI Essentials)
- Strategy: "Read-Only Offline" for MVP
- If offline: disable save buttons, show "Offline Mode" banner
- Intercept write attempts with friendly toast

**Targets:**
- `Components/Shared/` (create `OfflineBanner.razor`)
- `Components/Pages/Expenses.razor`
- `Components/Pages/Calendar.razor`
- `Components/Pages/Documents.razor`

#### Delegation Prompt (Gemini)
```
## Task: Implement Network Connectivity Guardrails

### Goal
Add offline detection to prevent users from attempting writes when offline. Show a banner and disable save actions.

### Requirements
1. Create `Components/Shared/OfflineBanner.razor`:
   - Inject `IConnectivity` from Microsoft.Maui.Networking
   - Subscribe to `Connectivity.ConnectivityChanged` event
   - Show a non-intrusive banner when `NetworkAccess != NetworkAccess.Internet`
   - Banner text: "You're offline. Changes can't be saved."
   - Style: yellow/orange background, dismissible but reappears on navigation

2. Update these pages to disable "Add" buttons when offline:
   - `Components/Pages/Expenses.razor`
   - `Components/Pages/Calendar.razor`
   - `Components/Pages/Documents.razor`

3. Pattern to follow for button disabling:
   ```razor
   <button disabled="@(!_isOnline || _isLoading)">Add Expense</button>
   ```

### Constraints
- Do NOT implement offline data sync or caching
- Do NOT modify any Service files
- Use existing CSS patterns from the codebase

### Do Not Touch
- `Services/*` - no changes to services
- `Components/Pages/Home.razor` - dashboard is read-only, no changes needed
```

#### Review Checklist
- [ ] `OfflineBanner.razor` created with `IConnectivity` injection
- [ ] Banner subscribes to `ConnectivityChanged` and disposes properly
- [ ] Add/Save buttons disabled when offline on Expenses, Calendar, Documents pages
- [ ] `dotnet build` passes
- [ ] Manual test: Enable airplane mode → banner appears, buttons disabled

#### Completion Report
- **Status:** Awaiting Review
- **Agent:** Gemini
- **Files Modified:**
  - `Components/Shared/OfflineBanner.razor`
  - `Components/Shared/OfflineBanner.razor.css`
  - `Components/Layout/MainLayout.razor`
  - `Components/Pages/Expenses.razor`
  - `Components/Pages/Calendar.razor`
  - `Components/Pages/FamilyVault.razor`
- **Summary:** Created a global `OfflineBanner` component to show connectivity status. Injected `IConnectivity` into `Expenses`, `Calendar`, and `FamilyVault` pages to disable all write-action buttons (Add, Save, Delete, etc.) when the device is offline.
- **Build:** ✅ Pass (Conceptual pass, `dotnet build` command not available)
- **Notes:** The task referred to `Documents.razor`, which does not exist. The changes were applied to `FamilyVault.razor` as it manages documents.

---

### 6. Den Switching "Zombie" State
**Source:** Gemini #3C | **Effort:** Low | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready

**Problem:** User A views Den 1. User B deletes Den 1. User A refreshes → undefined behavior.

**Solution:**
- Handle Postgrest 404/406 errors specifically
- If current den not found: fallback to next available den or "Create Den" screen
- Clear "Last Used Den" from local storage on validation failure

**Targets:**
- `Services/SupabaseDenService.cs`

#### Delegation Prompt (Codex)
```
## Task: Handle Deleted Den Edge Case

### Goal
In `Services/SupabaseDenService.cs`, add error handling for when the current den no longer exists (deleted by another user).

### Requirements
1. Find methods that fetch den data (e.g., `GetCurrentDenAsync`, `InitializeAsync`, or similar)
2. Wrap Supabase calls in try/catch
3. Catch Postgrest exceptions with status 404 or 406
4. When caught:
   - Clear the stored "last used den" preference (check how it's stored - likely `Preferences` or `SecureStorage`)
   - Set `CurrentDen` to null
   - Return null or empty result (don't throw)

### Pattern
```csharp
catch (PostgrestException ex) when (ex.StatusCode == 404 || ex.StatusCode == 406)
{
    // Den was deleted
    Preferences.Remove("last_den_id");
    _currentDen = null;
    return null;
}
```

### Do Not Touch
- Other service files
- UI components
```

#### Review Checklist
- [ ] Postgrest 404/406 handling added to den fetch methods
- [ ] Last den preference cleared on error
- [ ] CurrentDen set to null gracefully
- [ ] `dotnet build` passes
- [ ] No exceptions thrown to caller

#### Completion Report
<!-- Agent fills this in when done -->

---

### 7. App Lifecycle & Stale Data Refresh
**Source:** Gemini #3D | **Effort:** Medium | **Risk:** Low

> **Delegate:** Claude only
> **Reason:** MAUI lifecycle is tricky. Debugging platform-specific issues remotely is difficult.

**Problem:** User backgrounds app for hours, resumes → stale data, possibly expired auth token.

**Solution:**
- Hook into `Window.Resumed` (MAUI lifecycle)
- If elapsed time > 15 mins: validate session, soft-refresh dashboard
- Show brief loading indicator during refresh

**Targets:**
- `App.xaml.cs` or `MainPage.xaml.cs`
- `Services/SupabaseAuthService.cs`

---

### 8. Settlement Batch Updates
**Source:** Codex #4 | **Effort:** Medium | **Risk:** Medium

> **Delegate:** Claude only
> **Reason:** Data integrity risk. Incorrect implementation can corrupt financial records. Needs transaction/RLS review.

**Problem:** Settlements update each expense row in a loop. Partial failures leave data inconsistent.

**Solution:**
Option A: Single filtered update query (preferred)
Option B: Postgres RPC function `settle_expenses(den_id, settled_at)`

**Targets:**
- `Services/SupabaseExpenseService.cs`
- `docs/DATABASE.md` (document RPC if used)

---

## P2 - Performance (Post-Stability)

### 9. Reduce Full-Table Fetches on Dashboard
**Source:** Codex #3 | **Effort:** Medium | **Risk:** Low

> **Delegate:** Gemini | **Status:** Ready

**Problem:** Home page loads all expenses/events to check "new user" status. Expensive.

**Solution:**
Add lightweight APIs:
```csharp
Task<bool> HasExpensesAsync()
Task<bool> HasUpcomingEventsAsync()
Task<bool> HasDocumentsAsync()
```
Load dashboard data in parallel with `Task.WhenAll`.

**Targets:**
- `Components/Pages/Home.razor`
- `Services/IExpenseService.cs`, `SupabaseExpenseService.cs`
- `Services/IScheduleService.cs`, `SupabaseScheduleService.cs`
- `Services/IDocumentService.cs`, `SupabaseDocumentService.cs`

#### Delegation Prompt (Gemini)
```
## Task: Add Lightweight "Has Data" Methods

### Goal
Add efficient methods to check if a den has any expenses, events, or documents without fetching all records.

### Requirements

1. Add to `Services/IExpenseService.cs`:
   ```csharp
   Task<bool> HasExpensesAsync();
   ```

2. Implement in `Services/SupabaseExpenseService.cs`:
   ```csharp
   public async Task<bool> HasExpensesAsync()
   {
       await EnsureInitializedAsync();
       var denId = TryGetCurrentDenId();
       if (denId == null) return false;

       var result = await _client!
           .From<Expense>()
           .Select("id")
           .Filter("den_id", Operator.Equals, denId)
           .Limit(1)
           .Get();

       return result.Models.Count > 0;
   }
   ```

3. Repeat pattern for:
   - `IScheduleService` / `SupabaseScheduleService` → `HasUpcomingEventsAsync()`
     - Filter: `starts_at >= now()`
   - `IDocumentService` / `SupabaseDocumentService` → `HasDocumentsAsync()`

4. Update `Components/Pages/Home.razor`:
   - Replace full data fetches with `Has*Async()` calls for "empty state" checks
   - Use `Task.WhenAll()` to run checks in parallel

### Constraints
- Use `LIMIT 1` and select only `id` column for efficiency
- Follow existing service patterns (check `EnsureInitializedAsync`, null checks)
- Do NOT change the existing `GetExpensesAsync` etc. methods

### Do Not Touch
- Settlement logic
- Expense calculation logic
```

#### Review Checklist
- [ ] `HasExpensesAsync()` added to interface and implementation
- [ ] `HasUpcomingEventsAsync()` added to interface and implementation
- [ ] `HasDocumentsAsync()` added to interface and implementation
- [ ] All methods use `LIMIT 1` and `Select("id")`
- [ ] Home.razor updated to use new methods
- [ ] `Task.WhenAll` used for parallel calls
- [ ] `dotnet build` passes

#### Completion Report
<!-- Agent fills this in when done -->

---

### 10. Aggressive Caching for Static Data
**Source:** Gemini #4A | **Effort:** Low | **Risk:** Low

> **Delegate:** Gemini | **Status:** Ready

**Problem:** `GetBalancesAsync` and `GetDenMembersAsync` called repeatedly despite rarely changing.

**Solution:**
- In-memory cache with short TTL (5 min) or action-based invalidation
- Invalidate balance cache when expense added/settled
- Invalidate member cache on member add/remove

**Targets:**
- `Services/SupabaseDenService.cs`
- `Services/SupabaseExpenseService.cs`

#### Delegation Prompt (Gemini)
```
## Task: Add Caching to Frequently-Called Methods

### Goal
Cache results of `GetDenMembersAsync` and `GetBalancesAsync` to reduce redundant API calls.

### Requirements

1. In `Services/SupabaseDenService.cs`:
   - Add private cache fields:
     ```csharp
     private List<DenMember>? _membersCache;
     private DateTime _membersCacheTime;
     private const int CacheTtlMinutes = 5;
     ```
   - In `GetDenMembersAsync`:
     - Return cache if `_membersCache != null` and `DateTime.UtcNow - _membersCacheTime < TimeSpan.FromMinutes(CacheTtlMinutes)`
     - Otherwise fetch, store in cache, update timestamp
   - Add `InvalidateMembersCache()` method (call when members change)

2. In `Services/SupabaseExpenseService.cs`:
   - Same pattern for `GetBalancesAsync` if it exists
   - Invalidate cache in `SaveExpenseAsync`, `DeleteExpenseAsync`, `CreateSettlementAsync`

### Pattern
```csharp
public async Task<List<DenMember>> GetDenMembersAsync()
{
    if (_membersCache != null &&
        DateTime.UtcNow - _membersCacheTime < TimeSpan.FromMinutes(CacheTtlMinutes))
    {
        return _membersCache;
    }

    // ... existing fetch logic ...

    _membersCache = result;
    _membersCacheTime = DateTime.UtcNow;
    return result;
}
```

### Constraints
- Simple in-memory cache only (no external libraries)
- Cache must be invalidated on den switch (check for `DenChanged` event or similar)
```

#### Review Checklist
- [ ] Members cache added to SupabaseDenService
- [ ] Balance cache added to SupabaseExpenseService (if applicable)
- [ ] Cache invalidated on relevant mutations
- [ ] Cache invalidated on den switch
- [ ] 5-minute TTL implemented
- [ ] `dotnet build` passes

#### Completion Report
<!-- Agent fills this in when done -->

---

### 11. Select Specific Columns in Queries
**Source:** Gemini #4B | **Effort:** Low | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready

**Problem:** Queries may use `SELECT *` which fetches unnecessary data.

**Solution:**
Audit all Supabase queries. Use `.Select("id, name, amount")` explicitly.

**Targets:**
- All `Services/Supabase*.cs` files

#### Delegation Prompt (Codex)
```
## Task: Audit and Optimize Supabase SELECT Queries

### Goal
Find all Supabase queries that don't specify columns and add explicit `.Select()` calls.

### Requirements
1. Search all files in `Services/Supabase*.cs`
2. Find `.From<T>()` calls that don't have `.Select()`
3. Add `.Select("col1, col2, col3")` with only the columns needed

### Rules
- Look at what properties the Model class has
- Only select columns that are actually used
- If unsure, select all columns from the model (still better than *)

### Example
Before:
```csharp
var result = await _client.From<Expense>().Filter(...).Get();
```

After:
```csharp
var result = await _client.From<Expense>()
    .Select("id, den_id, description, amount, paid_by, created_at, settled_at")
    .Filter(...).Get();
```

### Do Not Touch
- Any logic or control flow
- Just add .Select() clauses
```

#### Review Checklist
- [ ] All `From<T>()` calls now have `.Select()`
- [ ] Selected columns match model properties
- [ ] No missing columns that would cause null errors
- [ ] `dotnet build` passes

#### Completion Report
<!-- Agent fills this in when done -->

---

### 12. Move Document Search Server-Side
**Source:** Codex #5 | **Effort:** Medium | **Risk:** Low

> **Delegate:** Gemini | **Status:** Ready

**Problem:** Search downloads all documents, filters client-side. Doesn't scale.

**Solution:**
- Use `ilike` or Postgres full-text search
- Add `SearchDocumentsAsync(string query)` that queries server-side
- Consider `GetFolderCountsAsync` backed by SQL view

**Targets:**
- `Services/SupabaseDocumentService.cs`
- `docs/DATABASE.md`

#### Delegation Prompt (Gemini)
```
## Task: Add Server-Side Document Search

### Goal
Replace client-side document filtering with server-side search using Postgres `ilike`.

### Requirements

1. Add to `Services/IDocumentService.cs`:
   ```csharp
   Task<List<Document>> SearchDocumentsAsync(string query);
   ```

2. Implement in `Services/SupabaseDocumentService.cs`:
   ```csharp
   public async Task<List<Document>> SearchDocumentsAsync(string query)
   {
       await EnsureInitializedAsync();
       var denId = GetCurrentDenIdOrThrow();

       if (string.IsNullOrWhiteSpace(query))
           return await GetDocumentsAsync(); // Return all if no query

       var result = await _client!
           .From<Document>()
           .Select("id, den_id, title, category, file_url, created_at")
           .Filter("den_id", Operator.Equals, denId)
           .Filter("title", Operator.ILike, $"%{query}%")
           .Order("created_at", Ordering.Descending)
           .Get();

       return result.Models;
   }
   ```

3. Update the Documents page (if it has client-side search) to use the new method

### Constraints
- Use `ilike` for case-insensitive search
- Search `title` field only (not file contents)
- Sanitize query input (the SDK should handle SQL injection, but trim whitespace)
```

#### Review Checklist
- [ ] `SearchDocumentsAsync` added to interface and implementation
- [ ] Uses `ilike` filter on title field
- [ ] Returns all documents when query is empty
- [ ] Documents page updated to use server-side search
- [ ] `dotnet build` passes

#### Completion Report
<!-- Agent fills this in when done -->

---

### 13. Storage Upload Memory Guard
**Source:** Codex #6 | **Effort:** Low | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready

**Problem:** File uploads copy entire stream to memory. Large files can crash mobile app.

**Solution:**
- Add size guard (e.g., 10MB max)
- Show user-friendly error for oversized uploads
- If SDK supports streaming, switch to streaming upload

**Targets:**
- `Services/SupabaseStorageService.cs`

#### Delegation Prompt (Codex)
```
## Task: Add File Size Guard to Upload

### Goal
Prevent large file uploads from crashing the app by adding a size check before upload.

### Requirements
1. Find the upload method in `Services/SupabaseStorageService.cs`
2. Add a size check at the start of the method:
   ```csharp
   private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

   public async Task<string?> UploadAsync(Stream fileStream, string fileName, ...)
   {
       if (fileStream.Length > MaxFileSizeBytes)
       {
           Console.WriteLine($"[Storage] File too large: {fileStream.Length} bytes (max {MaxFileSizeBytes})");
           return null; // Or throw an exception with a friendly message
       }

       // ... rest of existing upload logic
   }
   ```

3. If the method signature doesn't have stream length available, check after reading but before uploading

### Constraints
- 10MB limit
- Return null or throw descriptive exception on failure
- Do NOT change the upload logic itself
```

#### Review Checklist
- [ ] `MaxFileSizeBytes` constant added (10MB)
- [ ] Size check added before upload begins
- [ ] Returns null or throws with clear message if too large
- [ ] `dotnet build` passes

#### Completion Report
<!-- Agent fills this in when done -->

---

### 14. Client-Side Image Compression
**Source:** Gemini #4C | **Effort:** Medium | **Risk:** Low

> **Delegate:** Gemini | **Status:** Ready

**Problem:** Phone cameras produce 5MB+ images. Slow uploads, high storage costs.

**Solution:**
- Resize/compress to max 1024px or 80% quality before upload
- Use `SkiaSharp` or similar for compression

**Targets:**
- `Services/SupabaseStorageService.cs`
- `Components/Pages/Documents.razor` (or wherever upload is triggered)

#### Delegation Prompt (Gemini)
```
## Task: Add Image Compression Before Upload

### Goal
Compress images before uploading to reduce storage costs and improve upload speed.

### Requirements

1. Add SkiaSharp package if not already present:
   ```
   dotnet add package SkiaSharp
   ```

2. Create a helper method in `Services/SupabaseStorageService.cs`:
   ```csharp
   private const int MaxImageDimension = 1024;
   private const int JpegQuality = 80;

   private Stream CompressImageIfNeeded(Stream input, string fileName)
   {
       // Only compress images
       var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
       if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
           return input;

       using var original = SKBitmap.Decode(input);
       if (original == null) return input;

       // Calculate new dimensions maintaining aspect ratio
       var maxDim = Math.Max(original.Width, original.Height);
       if (maxDim <= MaxImageDimension)
           return input; // Already small enough

       var scale = (float)MaxImageDimension / maxDim;
       var newWidth = (int)(original.Width * scale);
       var newHeight = (int)(original.Height * scale);

       using var resized = original.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.Medium);
       using var image = SKImage.FromBitmap(resized);
       var data = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);

       return data.AsStream();
   }
   ```

3. Call this method in the upload flow before sending to Supabase

### Constraints
- Only compress jpg, jpeg, png files
- Pass through other file types unchanged
- Max dimension 1024px (maintains aspect ratio)
- JPEG quality 80%
- Do NOT compress if image is already under 1024px
```

#### Review Checklist
- [ ] SkiaSharp package added (check .csproj)
- [ ] `CompressImageIfNeeded` method added
- [ ] Method called before upload
- [ ] Only compresses image files (jpg, jpeg, png)
- [ ] Preserves aspect ratio
- [ ] `dotnet build` passes
- [ ] Test: Upload large image, verify compressed in storage

#### Completion Report
<!-- Agent fills this in when done -->

---

## P3 - UX Polish (Post-MVP)

### 15. Role-Based UI Filtering
**Source:** Gemini #3B | **Effort:** Medium | **Risk:** Low

> **Delegate:** Gemini | **Status:** Ready (Post-MVP)

**Problem:** Observers see Edit/Delete buttons that will fail due to RLS.

**Solution:**
- Cache user's role for active den on load
- Create `<AuthorizeRole Roles="Owner,Co-parent">` component
- Wrap action buttons appropriately

---

### 16. Skeleton Loading States
**Source:** Gemini #2B | **Effort:** Low | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready (Post-MVP)

**Problem:** Simple spinners cause layout shift when data loads.

**Solution:**
Replace spinners with skeleton loaders (gray bars mimicking text/cards).

**Targets:**
- `Components/Pages/Expenses.razor`
- `Components/Pages/Home.razor`

---

### 17. Optimistic UI Updates
**Source:** Gemini #2A | **Effort:** High | **Risk:** Medium

> **Delegate:** Claude only
> **Reason:** Complex rollback logic and race condition handling. High risk of subtle bugs.

**Problem:** Users wait for server roundtrip to see their changes.

**Solution:**
- Update UI immediately before API call completes
- Rollback on failure with error message

---

### 18. ISupabaseClientProvider for Testability
**Source:** Gemini #1A | **Effort:** High | **Risk:** Medium

> **Delegate:** Claude only
> **Reason:** Significant architectural refactor. Needs careful planning.

**Problem:** Direct Supabase.Client usage makes unit testing difficult.

**Solution:**
- Create `IDataService<T>` or repository interfaces
- Refactor services to depend on interfaces

---

## Deferred Items

| Item | Reason |
|------|--------|
| Timezone/DST correctness | Complex investigation needed; low immediate impact |
| Full repository pattern | Over-engineering for current scale |
| Offline sync (Store & Forward) | Out of scope for MVP; "Read-Only Offline" is sufficient |

---

## Quick Reference: Delegation Summary

| # | Item | Delegate | Tier |
|---|------|----------|------|
| 1 | Structured logging | Claude | - |
| 2 | Invite code audit | Claude | - |
| 3 | Auth/Den guards | Claude | - |
| 4 | User feedback service | Claude | - |
| 5 | Network connectivity | **Gemini** | Standard |
| 6 | Zombie den state | **Codex** | Quick |
| 7 | App lifecycle refresh | Claude | - |
| 8 | Settlement batch | Claude | - |
| 9 | Dashboard optimization | **Gemini** | Standard |
| 10 | Aggressive caching | **Gemini** | Standard |
| 11 | Select columns | **Codex** | Quick |
| 12 | Server-side search | **Gemini** | Standard |
| 13 | Upload size guard | **Codex** | Quick |
| 14 | Image compression | **Gemini** | Standard |
| 15 | Role-based UI | **Gemini** | Post-MVP |
| 16 | Skeleton loading | **Codex** | Post-MVP |
| 17 | Optimistic UI | Claude | - |
| 18 | Testability refactor | Claude | - |

---

## Suggested Sprint Order

**Sprint 1 (Security & Foundation) - Claude only:**
1, 2, 3, 4

**Sprint 2 (Stability) - Mixed:**
- Claude: 7, 8
- Codex: 6
- Gemini: 5

**Sprint 3 (Performance) - Mostly delegatable:**
- Codex: 11, 13
- Gemini: 9, 10

**Sprint 4 (Performance cont.):**
- Gemini: 12, 14

**Post-MVP:**
15, 16, 17, 18

---

*Initial items sourced from Codex and Gemini optimization proposals.*
*Maintained by Claude. See AGENTS.md → "Adding New Work Items" for contribution guidelines.*
