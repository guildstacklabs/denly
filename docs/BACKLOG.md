# Denly Product Backlog

## Quick Reference: Delegation Summary

| ID | Feature | Delegate | Status |
|----|---------|----------|--------|
| P0-7 | App color scheme | Codex | Ready |
| P0-8 | Unit test foundation | Claude | Ready |
| P1-1 | Calendar reminders | Claude | Ready |
| P1-2 | Exportable reports | Gemini | Ready |
| P1-3 | Info Bank expansion | Gemini | Ready |
| P1-7 | Flexible expense splits | Codex | Ready |
| P1-8 | Calendar sync | Claude | Ready |
| P1-9 | Scheduled messages | TBD | Blocked (messaging) |
| P2-1 | Settlement confirmation | Codex | Ready |
| P2-2 | Third-party view access | Gemini | Ready |
| P2-3 | Multi-child color coding | Codex | Ready |
| P2-4 | Full data export | Gemini | Ready |
| P2-5 | Daily agenda email | Codex | Ready |
| P2-6 | Per-person calendar colors | Codex | Ready |
| P2-10 | Message drafts auto-save | TBD | Blocked (messaging) |
| P2-11 | Calendar "viewed" tracking | Codex | Ready |

---

## P0: Pre-Launch Critical

| ID | Feature | Description | Complexity | Status |
|----|---------|-------------|------------|--------|
| P0-1 | App icon + splash screen | Coral Reef palette, brand identity | Low | ✅ Done |
| P0-2 | Android testing | Full testing pass, bug fixes | Medium | In Progress |
| P0-3 | Supabase auth setup | Email/password + Google auth | Medium | In Progress |
| P0-4 | Cloud sync | Sync calendar, expenses, vault between co-parents | High | In Progress |
| P0-5 | Onboarding flow | First-run experience, invite co-parent flow | Medium | In Progress |
| P0-6 | Push notifications | Reliable notification system (OFW's #1 failure) | Medium | Not Started |
| P0-7 | App color scheme | Update app UI colors to match Coral Reef palette: primary coral (#E07A5F), teal (#3D8B8B), seafoam (#81B29A), gold (#F2CC8F), warm background (#FFF9F0) | Low | Not Started |
| P0-8 | Unit test foundation | Create test project, Supabase abstraction, initial service tests | Medium | Not Started |

---

### P0-7: App Color Scheme
**Source:** Brand identity | **Effort:** Low | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready
> **Reason:** Straightforward CSS/styling task with clear specifications

**Problem:** The app UI doesn't yet reflect the Coral Reef brand palette established in P0-1 (app icon + splash screen). Colors need to be consistent across all UI elements.

**Solution:**
- Define CSS custom properties for the Coral Reef palette
- Update all UI components to use the new color variables
- Ensure accessibility (contrast ratios meet WCAG AA)
- Apply consistently across light mode (dark mode can follow later)

**Color Palette:**
| Name | Hex | Usage |
|------|-----|-------|
| Coral (Primary) | #E07A5F | Primary buttons, links, accents |
| Teal | #3D8B8B | Secondary actions, icons |
| Seafoam | #81B29A | Success states, positive indicators |
| Gold | #F2CC8F | Warnings, highlights, badges |
| Warm Background | #FFF9F0 | Page backgrounds |
| Text Dark | #2D3748 | Primary text |
| Text Light | #718096 | Secondary text |

**Targets:**
- `wwwroot/css/app.css` (add CSS custom properties)
- `Components/Layout/MainLayout.razor` (apply background)
- `Components/Layout/NavMenu.razor` (nav styling)
- All page components (buttons, links, badges)

#### Delegation Prompt (Codex)
```
Update the Denly app to use the Coral Reef color palette consistently.

CONTEXT:
- .NET MAUI Blazor Hybrid app
- CSS is in wwwroot/css/app.css
- Components use standard Blazor patterns

COLOR PALETTE:
- Coral (Primary): #E07A5F - primary buttons, links, active states
- Teal: #3D8B8B - secondary buttons, icons, nav highlights
- Seafoam: #81B29A - success states, confirmations
- Gold: #F2CC8F - warnings, pending states, badges
- Warm Background: #FFF9F0 - page backgrounds
- Text Dark: #2D3748 - primary text
- Text Light: #718096 - secondary/muted text

REQUIREMENTS:
1. Add CSS custom properties in app.css:
   --color-primary: #E07A5F;
   --color-secondary: #3D8B8B;
   --color-success: #81B29A;
   --color-warning: #F2CC8F;
   --color-background: #FFF9F0;
   --color-text: #2D3748;
   --color-text-muted: #718096;

2. Update component styles to use these variables:
   - Buttons: primary uses --color-primary, secondary uses --color-secondary
   - Links: use --color-primary with hover state
   - Page backgrounds: use --color-background
   - Navigation: use --color-secondary for active states
   - Badges/status indicators: use appropriate semantic colors

3. Ensure accessibility:
   - Text on backgrounds must meet WCAG AA (4.5:1 for normal text)
   - Interactive elements must have visible focus states
   - Add slight darkening on hover states

CONSTRAINTS:
- Do NOT change component structure, only styling
- Do NOT add new CSS frameworks or libraries
- Maintain existing responsive behavior
- Use CSS custom properties (not hardcoded hex values)

DELIVERABLES:
- Updated wwwroot/css/app.css
- Updated component .razor.css files as needed
- Build must pass
- Visual verification: app looks cohesive with Coral Reef palette
```

#### Review Checklist
- [ ] All CSS custom properties defined in app.css
- [ ] No hardcoded color values in component styles
- [ ] Primary buttons use coral color
- [ ] Background is warm off-white (#FFF9F0)
- [ ] Text is readable (contrast check)
- [ ] Hover/focus states work properly
- [ ] App looks visually cohesive

#### Completion Report
<!-- Agent fills in when done -->

---

### P0-8: Unit Test Foundation
**Source:** Multi-agent quality assurance | **Effort:** Medium | **Risk:** Low

> **Delegate:** Claude | **Status:** Ready
> **Reason:** Complex architectural changes requiring careful refactoring of service dependencies while maintaining existing functionality

**Problem:** Multiple LLM agents (Claude, Codex, Gemini) contribute code without automated verification. Bugs introduced by one agent may go undetected until manual testing, wasting significant debugging time. The codebase has zero automated tests.

**Solution:**
- Create `Denly.Tests` xUnit project
- Add `ISupabaseClientWrapper` abstraction to enable mocking
- Implement 3 initial high-value tests:
  - Expense balance calculations
  - Den guardrails (empty results when no den)
  - Auth session restore (no-throw on missing storage)
- Configure CI test step

**Targets:**
- `Denly.Tests/Denly.Tests.csproj` (new)
- `Denly.Tests/Services/ExpenseServiceTests.cs` (new)
- `Denly.Tests/Services/DenServiceTests.cs` (new)
- `Denly.Tests/Services/AuthServiceTests.cs` (new)
- `Denly.Tests/Mocks/MockSupabaseClient.cs` (new)
- `Services/ISupabaseClientWrapper.cs` (new)
- `Services/SupabaseClientWrapper.cs` (new)
- `Services/SupabaseServiceBase.cs` (modify to use wrapper)
- `MauiProgram.cs` (register wrapper)
- `Denly.sln` (add test project)

#### Delegation Prompt (Claude)
```
Create a unit test foundation for Denly, a .NET MAUI Blazor Hybrid co-parenting app.

CONTEXT:
- Currently zero automated tests
- Multiple LLM agents (Codex, Gemini) contribute code that needs verification
- Supabase client is accessed via IAuthService.GetSupabaseClient() - not mockable
- Static dependencies (SecureStorage, WebAuthenticator) block testing
- See docs/TESTING-strategy.md for full architecture analysis

REQUIREMENTS:
1. Create Denly.Tests project:
   - xUnit test framework
   - NSubstitute for mocking (preferred) or Moq
   - Reference main Denly project
   - Add to Denly.sln

2. Create ISupabaseClientWrapper abstraction:
   - Wrap the Supabase client operations used by services
   - Methods: GetTable<T>(), From(tableName), Auth property
   - Implement SupabaseClientWrapper that delegates to real client
   - Register in MauiProgram.cs DI container

3. Implement 3 initial test files:

   ExpenseServiceTests.cs:
   - Test balance calculation with various expense splits
   - Test 50/50 split (default)
   - Test custom splits (60/40, 70/30)
   - Test empty expense list returns zero balance

   DenServiceTests.cs:
   - Test GetCurrentDen returns null when user has no den
   - Test GetChildren returns empty list when no den
   - Test den membership checks

   AuthServiceTests.cs:
   - Test session restore doesn't throw when SecureStorage is empty
   - Test GetCurrentUser returns null when not authenticated
   - Mock ISecureStorage for testability

4. Update SupabaseServiceBase:
   - Accept ISupabaseClientWrapper via constructor injection
   - Maintain backward compatibility during transition

CONSTRAINTS:
- Do NOT break existing functionality
- Do NOT modify Supabase table schemas
- Tests must be deterministic (no flakiness)
- Use structured logging in test helpers
- Follow existing code style (file-scoped namespaces, nullable types)

TEST PATTERNS:
- Arrange-Act-Assert structure
- Descriptive test names: MethodName_Scenario_ExpectedResult
- One assertion per test (prefer)
- Mock external dependencies, not internal logic

DELIVERABLES:
- Denly.Tests project with 3 test files
- ISupabaseClientWrapper interface and implementation
- Updated SupabaseServiceBase
- Updated MauiProgram.cs registration
- All tests pass: `dotnet test`
- Build passes: `dotnet build`
```

#### Review Checklist
- [ ] Test project builds successfully
- [ ] All tests pass with `dotnet test`
- [ ] ISupabaseClientWrapper properly abstracts Supabase operations
- [ ] Existing app functionality unchanged
- [ ] Tests are deterministic (no flakiness)
- [ ] Test names clearly describe what's being tested
- [ ] Mocks are properly configured
- [ ] No hardcoded test data that could break

#### Completion Report
<!-- Agent fills in when done -->

---

## P1: Competitive Parity

### P1-1: Calendar Reminders
**Source:** OFW competitive analysis | **Effort:** Medium | **Risk:** Medium

> **Delegate:** Claude | **Status:** Ready
> **Reason:** Platform-specific notification APIs require careful architecture decisions; iOS/Android have different permission models

**Problem:** Users miss important handoffs and appointments because there's no reminder system. OurFamilyWizard has this and it's a table-stakes feature for co-parenting apps.

**Solution:**
- Create `INotificationService` interface with platform-agnostic API
- Implement platform-specific notification scheduling (iOS: `UNUserNotificationCenter`, Android: `AlarmManager` + `NotificationCompat`)
- Add reminder settings per event type (handoffs, appointments, general)
- Store reminder preferences in user settings
- Default: 1 hour before events, 1 day before handoffs

**Targets:**
- `Services/INotificationService.cs` (new)
- `Services/NotificationService.cs` (new - shared logic)
- `Platforms/iOS/NotificationService.cs` (new)
- `Platforms/Android/NotificationService.cs` (new)
- `Components/Pages/Calendar.razor` (add reminder toggle to event creation)
- `Components/Pages/Settings.razor` (add reminder preferences section)
- `MauiProgram.cs` (register notification service)

#### Delegation Prompt (Claude)
```
Implement calendar reminders for Denly, a .NET MAUI Blazor Hybrid co-parenting app.

CONTEXT:
- Events are stored in Supabase `events` table (see Models/Event.cs)
- App targets iOS and Android
- Follow existing service pattern: interface + platform implementations

REQUIREMENTS:
1. Create INotificationService interface:
   - ScheduleReminder(Event event, TimeSpan beforeEvent)
   - CancelReminder(string eventId)
   - RequestPermissionAsync() -> bool
   - CheckPermissionAsync() -> bool

2. Implement platform-specific services:
   - iOS: Use UNUserNotificationCenter
   - Android: Use AlarmManager with NotificationCompat
   - Handle app restart (reminders must persist)

3. Add UI in Calendar.razor:
   - Toggle "Remind me" when creating/editing events
   - Dropdown for reminder time: 15min, 1hr, 1day, custom

4. Add Settings section:
   - Default reminder times per event type
   - Master enable/disable toggle
   - Permission status indicator

CONSTRAINTS:
- Do NOT modify Models/Event.cs (reminders are local, not synced)
- Do NOT touch auth or Supabase services
- Use structured logging (ILogger), never Console.WriteLine
- Request notification permission only when user enables reminders

DELIVERABLES:
- All new/modified files listed above
- Build must pass
- Test manually: create event with reminder, verify notification fires
```

#### Review Checklist
- [ ] Permission request only triggers on user action
- [ ] Reminders survive app restart
- [ ] No PII in notification content (no child names in notification body)
- [ ] Android: Notification channel created properly
- [ ] iOS: Permission prompt text is user-friendly
- [ ] Settings UI matches existing app style

#### Completion Report
<!-- Agent fills in when done -->

---

### P1-2: Exportable Reports
**Source:** Legal/custody requirements | **Effort:** Medium | **Risk:** Low

> **Delegate:** Gemini | **Status:** Ready
> **Reason:** Straightforward data formatting task following established patterns

**Problem:** Co-parents often need to provide expense and calendar history to lawyers, mediators, or courts. Currently they must screenshot or manually compile this data.

**Solution:**
- Create `IReportService` for generating PDF/CSV reports
- Support three report types: Expenses, Calendar, Combined
- Add date range filtering
- Include summary statistics (totals, averages)
- Add "Export" button to Settings page

**Targets:**
- `Services/IReportService.cs` (new)
- `Services/ReportService.cs` (new)
- `Components/Pages/Settings.razor` (add Export section)

#### Delegation Prompt (Gemini)
```
Add exportable reports to Denly, a .NET MAUI Blazor Hybrid co-parenting app.

CONTEXT:
- Expenses stored in Supabase (see Models/Expense.cs, Services/IExpenseService.cs)
- Events stored in Supabase (see Models/Event.cs, Services/IScheduleService.cs)
- Use existing service injection pattern

REQUIREMENTS:
1. Create IReportService interface:
   - GenerateExpenseReportAsync(DateTime start, DateTime end, string format) -> byte[]
   - GenerateCalendarReportAsync(DateTime start, DateTime end, string format) -> byte[]
   - GenerateCombinedReportAsync(DateTime start, DateTime end, string format) -> byte[]
   - Supported formats: "pdf", "csv"

2. Implement ReportService:
   - Inject IExpenseService and IScheduleService
   - For PDF: Use QuestPDF or similar (check if already in project, else use basic HTML-to-PDF)
   - For CSV: Simple comma-separated with headers
   - Include summary: total expenses, expense breakdown by category, event counts by type

3. Add UI in Settings.razor:
   - New "Reports" section
   - Date range picker (start/end)
   - Report type dropdown (Expenses, Calendar, Combined)
   - Format dropdown (PDF, CSV)
   - "Generate Report" button
   - Use platform share sheet to export file

CONSTRAINTS:
- Do NOT modify expense or calendar services
- Do NOT add new NuGet packages without noting in completion report
- Use structured logging (ILogger)
- Sanitize any user data in reports (no raw user IDs)

DELIVERABLES:
- IReportService.cs, ReportService.cs
- Updated Settings.razor
- Build must pass
```

#### Review Checklist
- [ ] PDF is readable and well-formatted
- [ ] CSV opens correctly in Excel/Numbers
- [ ] Date range filtering works correctly
- [ ] No user IDs exposed in exported files (use display names)
- [ ] Share sheet works on both iOS and Android
- [ ] Large date ranges don't cause memory issues

#### Completion Report
<!-- Agent fills in when done -->

---

### P1-3: Info Bank Expansion
**Source:** User research | **Effort:** Low | **Risk:** Low

> **Delegate:** Gemini | **Status:** Ready
> **Reason:** Straightforward model extension and UI work following existing patterns

**Problem:** The Child model only stores name and birthdate. Co-parents need to track critical info like clothing sizes, allergies, school details, and emergency contacts—info that changes and needs to be shared.

**Solution:**
- Extend Child model with additional fields
- Create dedicated Info Bank page for viewing/editing child details
- Organize info into collapsible sections
- All fields optional to avoid overwhelming new users

**Targets:**
- `Models/Child.cs` (extend with new fields)
- `Components/Pages/InfoBank.razor` (new page)
- `Components/Pages/InfoBank.razor.css` (new styles)
- `Components/Layout/NavMenu.razor` (add Info Bank nav item)
- `Services/IDenService.cs` (add UpdateChildAsync if missing)
- `Services/SupabaseDenService.cs` (implement UpdateChildAsync)

#### Delegation Prompt (Gemini)
```
Expand the Info Bank feature in Denly, a .NET MAUI Blazor Hybrid co-parenting app.

CONTEXT:
- Child model exists at Models/Child.cs (currently: Id, DenId, Name, BirthDate, Color, CreatedAt)
- Children are loaded via IDenService
- App uses Supabase for storage

REQUIREMENTS:
1. Extend Child model with nullable fields:
   - ClothingSizeTop (string?)
   - ClothingSizeBottom (string?)
   - ShoeSize (string?)
   - Allergies (string?) - comma-separated or free text
   - MedicalNotes (string?)
   - SchoolName (string?)
   - SchoolGrade (string?)
   - TeacherName (string?)
   - SchoolPhone (string?)
   - EmergencyContact1Name (string?)
   - EmergencyContact1Phone (string?)
   - EmergencyContact1Relation (string?)
   - EmergencyContact2Name (string?)
   - EmergencyContact2Phone (string?)
   - EmergencyContact2Relation (string?)

2. Create InfoBank.razor page:
   - Route: /info-bank
   - List all children in the den
   - Tap child to expand/edit their info
   - Organize into sections: Basic, Clothing, Medical, School, Emergency Contacts
   - Each section collapsible
   - Edit button per section, inline editing
   - Save button commits changes

3. Add navigation:
   - Add "Info Bank" to NavMenu.razor between Calendar and Expenses
   - Use appropriate icon (info circle or similar)

4. Ensure IDenService has UpdateChildAsync(Child child):
   - If missing, add to interface and implement in SupabaseDenService

CONSTRAINTS:
- All new fields must be nullable (existing children won't have data)
- Do NOT change existing Child fields
- Match existing app styling (see other pages for patterns)
- Use structured logging
- Supabase migration: Note in completion report that DB schema needs updating

DATABASE NOTE:
New columns needed in `children` table - list them in completion report for manual migration.

DELIVERABLES:
- Updated Child.cs
- New InfoBank.razor and InfoBank.razor.css
- Updated NavMenu.razor
- Updated IDenService.cs and SupabaseDenService.cs if needed
- Build must pass
```

#### Review Checklist
- [ ] All new fields are nullable
- [ ] Existing children display without errors (null handling)
- [ ] Edits save correctly to Supabase
- [ ] UI is consistent with other pages
- [ ] Nav menu icon fits the design
- [ ] Completion report lists required DB migrations

#### Completion Report
<!-- Agent fills in when done -->

---

### P1-7: Flexible Expense Splits
**Source:** Competitive analysis (2houses advantage) | **Effort:** Low | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready
> **Reason:** Well-scoped model and UI change, isolated to expense feature

**Problem:** OurFamilyWizard only supports 50/50 or 100% expense splits. Real co-parenting situations often have different arrangements (60/40, 70/30, etc.) based on income disparity or custody agreements.

**Solution:**
- Add configurable default split percentage to Den settings
- Allow per-expense override of split percentage
- Update expense calculations to use custom splits
- Show split percentage in expense history

**Targets:**
- `Models/Den.cs` (add DefaultSplitPercent field)
- `Models/Expense.cs` (add SplitPercent field)
- `Services/IExpenseService.cs` (update balance calculations)
- `Services/SupabaseExpenseService.cs` (implement split logic)
- `Components/Pages/Expenses.razor` (add split selector UI)
- `Components/Pages/Settings.razor` (add default split setting)

#### Delegation Prompt (Codex)
```
Add flexible expense splits to Denly expense tracking.

CONTEXT:
- Expense model exists in Models/Expense.cs
- Has: Id, DenId, Amount, Description, Category, PaidBy, CreatedAt
- Expenses.razor displays expenses and calculates who owes whom
- Currently assumes 50/50 split

REQUIREMENTS:
1. Add to Den model:
   - DefaultSplitPercent (int, default 50) - the percentage Parent A pays
   - Parent B pays (100 - DefaultSplitPercent)

2. Add to Expense model:
   - SplitPercent (int?) - nullable, uses Den default if null
   - Computed property EffectiveSplitPercent that returns SplitPercent ?? Den.DefaultSplitPercent

3. Update expense creation UI in Expenses.razor:
   - Add split percentage selector when logging expense
   - Preset options: 50/50, 60/40, 70/30, 80/20, 100/0, Custom
   - Custom allows entering any percentage 0-100
   - Default to Den's DefaultSplitPercent

4. Update balance calculations:
   - If Parent A paid $100 and split is 60/40:
     - Parent A's share: $60
     - Parent B's share: $40
     - Parent B owes Parent A: $40
   - Update running balance display

5. Add to Settings.razor:
   - "Default Expense Split" section
   - Dropdown or slider for default percentage
   - Explain: "Parent A pays X%, Parent B pays Y%"
   - Save to Den record

CONSTRAINTS:
- Do NOT break existing expenses (treat null SplitPercent as 50%)
- Split must be 0-100 integer
- Use structured logging
- Keep expense history working

DATABASE NOTE:
- Add `default_split_percent` to `dens` table (int, default 50)
- Add `split_percent` to `expenses` table (int, nullable)

DELIVERABLES:
- Updated Models/Den.cs
- Updated Models/Expense.cs
- Updated IExpenseService.cs and SupabaseExpenseService.cs
- Updated Expenses.razor
- Updated Settings.razor
- Build must pass
```

#### Review Checklist
- [ ] Default split saves to Den correctly
- [ ] Per-expense split override works
- [ ] Balance calculations are correct with custom splits
- [ ] Existing expenses (null split) default to 50/50
- [ ] UI clearly shows who pays what percentage
- [ ] Settings UI explains split direction

#### Completion Report
<!-- Agent fills in when done -->

---

### P1-8: Calendar Sync
**Source:** Top user request across all apps | **Effort:** Medium | **Risk:** Medium

> **Delegate:** Claude | **Status:** Ready
> **Reason:** Platform-specific calendar APIs require careful architecture; iOS/Android have different calendar access patterns

**Problem:** Users want their Denly events to appear in their native calendar apps (Google Calendar, Apple Calendar) without manual entry. This is the #1 requested feature across all co-parenting apps.

**Solution:**
- Export: Generate .ics files for download/share
- Import: Allow importing .ics files to add events
- Sync: Optional two-way sync with device calendar (stretch goal)
- Start with export/import, add sync later

**Targets:**
- `Services/ICalendarSyncService.cs` (new)
- `Services/CalendarSyncService.cs` (new)
- `Components/Pages/Calendar.razor` (add export/import buttons)
- `Components/Pages/Settings.razor` (add calendar sync preferences)

#### Delegation Prompt (Claude)
```
Add calendar sync capabilities to Denly, a .NET MAUI Blazor Hybrid co-parenting app.

CONTEXT:
- Events stored in Supabase (see Models/Event.cs, Services/IScheduleService.cs)
- Event has: Id, DenId, Title, Description, StartTime, EndTime, EventType, ChildId, CreatedBy
- App targets iOS and Android
- Users want events in Google Calendar / Apple Calendar

REQUIREMENTS:
1. Create ICalendarSyncService interface:
   - ExportToIcsAsync(DateTime? start, DateTime? end) -> Task<byte[]>
   - ExportEventToIcsAsync(string eventId) -> Task<byte[]>
   - ImportFromIcsAsync(byte[] icsData) -> Task<List<Event>>
   - GetDeviceCalendarsAsync() -> Task<List<DeviceCalendar>> (for future sync)

2. Implement CalendarSyncService:
   - Generate valid iCalendar (.ics) format
   - Include: VEVENT with SUMMARY, DESCRIPTION, DTSTART, DTEND, UID
   - UID should be stable (use event ID)
   - Handle all-day events vs timed events
   - Parse incoming .ics files to create Event objects

3. Add UI in Calendar.razor:
   - "Export" button in toolbar/menu
   - Options: "Export All", "Export This Month", "Export Selected"
   - "Import" button that opens file picker
   - Preview imported events before confirming

4. Add to Settings.razor:
   - "Calendar Sync" section
   - Export options (date range, event types)
   - Future: Device calendar selection for sync

ICS FORMAT EXAMPLE:
```
BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Denly//Co-Parenting//EN
BEGIN:VEVENT
UID:event-123@denly.app
DTSTART:20250115T090000Z
DTEND:20250115T100000Z
SUMMARY:Soccer Practice
DESCRIPTION:Bring cleats and water bottle
END:VEVENT
END:VCALENDAR
```

CONSTRAINTS:
- Do NOT implement real-time sync in MVP (just import/export)
- Do NOT modify Event model
- Use platform share sheet for export
- Use platform file picker for import
- Use structured logging (ILogger)
- Handle timezone properly (store UTC, display local)

DELIVERABLES:
- Services/ICalendarSyncService.cs (new)
- Services/CalendarSyncService.cs (new)
- Updated Calendar.razor
- Updated Settings.razor if needed
- Build must pass
- Test: export events, import into Google Calendar, verify correct
```

#### Review Checklist
- [ ] Exported .ics opens in Google Calendar
- [ ] Exported .ics opens in Apple Calendar
- [ ] Import parses .ics correctly
- [ ] Event times are correct (timezone handling)
- [ ] All-day events work correctly
- [ ] UID is stable (re-export doesn't duplicate)
- [ ] Large exports don't crash app

#### Completion Report
<!-- Agent fills in when done -->

---

### Additional P1 Items (Blocked)

| ID | Feature | Description | Rationale | Status |
|----|---------|-------------|-----------|--------|
| P1-9 | Scheduled messages | Send message at future date/time | Requested in OFW reviews | Blocked (P3-8) |

> **Note:** P1-9 blocked by messaging feature (P3-8). Will be prepared when messaging is implemented.

---

## P2: Differentiators

### P2-1: Settlement Confirmation
**Source:** Expense tracking gaps | **Effort:** Low | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready
> **Reason:** Well-scoped, isolated change to existing model and UI

**Problem:** When one parent logs a settlement payment, there's no confirmation from the receiving parent. This can lead to disputes about whether payment was actually received.

**Solution:**
- Add `confirmed_at` and `confirmed_by` fields to Settlement model
- Show "Pending Confirmation" badge on unconfirmed settlements
- Add "Confirm Receipt" button for the receiving party
- Display confirmation status in settlement history

**Targets:**
- `Models/Expense.cs` (Settlement class - add ConfirmedAt, ConfirmedBy)
- `Services/IExpenseService.cs` (add ConfirmSettlementAsync)
- `Services/SupabaseExpenseService.cs` (implement ConfirmSettlementAsync)
- `Components/Pages/Expenses.razor` (add confirmation UI)

#### Delegation Prompt (Codex)
```
Add settlement confirmation to Denly expense tracking.

CONTEXT:
- Settlement model exists in Models/Expense.cs
- Has: Id, DenId, FromUserId, ToUserId, Amount, Note, CreatedBy, CreatedAt
- Expenses.razor displays settlements

REQUIREMENTS:
1. Add to Settlement class:
   - ConfirmedAt (DateTime?) - when recipient confirmed
   - ConfirmedBy (string?) - user ID who confirmed
   - IsConfirmed (bool, computed) - true if ConfirmedAt has value

2. Add to IExpenseService:
   - ConfirmSettlementAsync(string settlementId) -> Task<bool>

3. Implement in SupabaseExpenseService:
   - Update settlement with confirmed_at = now, confirmed_by = current user
   - Only allow if current user is ToUserId

4. Update Expenses.razor:
   - Show "Pending" badge on unconfirmed settlements
   - Show "Confirm Receipt" button only to ToUserId
   - Show "Confirmed" badge with date on confirmed settlements

CONSTRAINTS:
- Only ToUserId can confirm (the person receiving money)
- Do NOT modify Expense class, only Settlement
- Keep existing settlement functionality intact
- Use structured logging

DATABASE NOTE:
New columns needed: confirmed_at (timestamptz), confirmed_by (uuid references profiles)

DELIVERABLES:
- Updated Models/Expense.cs (Settlement class only)
- Updated IExpenseService.cs
- Updated SupabaseExpenseService.cs
- Updated Expenses.razor
- Build must pass
```

#### Review Checklist
- [ ] Only ToUserId sees "Confirm" button
- [ ] Confirmation persists correctly
- [ ] Badge styling matches app theme
- [ ] Cannot confirm same settlement twice
- [ ] Completion report notes DB migration

#### Completion Report
<!-- Agent fills in when done -->

---

### P2-2: Third-Party View Access
**Source:** User requests | **Effort:** Medium | **Risk:** Medium

> **Delegate:** Gemini | **Status:** Ready
> **Reason:** New feature but follows established auth patterns; moderate complexity

**Problem:** Grandparents, nannies, and other caregivers often need to see the custody schedule but shouldn't have full app access. Currently there's no way to share limited access.

**Solution:**
- Create "Viewer" role distinct from co-parent
- Viewers can see calendar (read-only), cannot see expenses or documents
- Invite flow similar to co-parent but creates viewer account
- Viewers have simplified nav (calendar only)

**Targets:**
- `Models/DenMember.cs` (add Role enum: CoParent, Viewer)
- `Services/IDenService.cs` (add InviteViewerAsync)
- `Services/SupabaseDenService.cs` (implement viewer invite)
- `Components/Pages/Settings.razor` (add "Invite Viewer" section)
- `Components/Layout/NavMenu.razor` (conditionally hide items for viewers)
- `Components/Pages/Calendar.razor` (hide edit controls for viewers)

#### Delegation Prompt (Gemini)
```
Add third-party viewer access to Denly, a .NET MAUI Blazor Hybrid co-parenting app.

CONTEXT:
- Den members are tracked via DenMember model
- Current flow: co-parents have full access
- Auth handled via IAuthService

REQUIREMENTS:
1. Add Role to DenMember:
   - Create enum: DenMemberRole { CoParent, Viewer }
   - Add Role property to DenMember (default: CoParent)
   - Existing members should be treated as CoParent

2. Implement viewer invite:
   - Add InviteViewerAsync(string email) to IDenService
   - Creates invite with role=viewer
   - Viewer joins via existing join flow but gets Viewer role

3. Update NavMenu.razor:
   - Inject service to get current user's role
   - If Viewer: show only Calendar, hide Expenses, Vault, Settings (except logout)
   - If CoParent: show all (current behavior)

4. Update Calendar.razor:
   - If Viewer: hide "Add Event" button
   - If Viewer: hide edit/delete on events
   - Calendar remains fully visible

5. Update Settings.razor:
   - Add "Viewers" section showing current viewers
   - Add "Invite Viewer" with email input
   - Show "Remove" button next to each viewer

CONSTRAINTS:
- Viewers can ONLY see calendar
- Do NOT give viewers access to expenses, documents, or settings (except logout)
- Maintain existing co-parent functionality
- Use structured logging
- Check role before any sensitive operation

DATABASE NOTE:
Add `role` column to `den_members` table (text, default 'coparent')

DELIVERABLES:
- Updated DenMember.cs
- Updated IDenService.cs and SupabaseDenService.cs
- Updated NavMenu.razor
- Updated Calendar.razor
- Updated Settings.razor
- Build must pass
```

#### Review Checklist
- [ ] Viewers cannot navigate to Expenses or Vault pages
- [ ] Viewers cannot create/edit/delete events
- [ ] Viewers can view full calendar
- [ ] Remove viewer works correctly
- [ ] Existing co-parents unaffected
- [ ] Role persists across sessions

#### Completion Report
<!-- Agent fills in when done -->

---

### P2-3: Multi-Child Color Coding
**Source:** Multi-child families | **Effort:** Low | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready
> **Reason:** Simple UI enhancement, isolated change

**Problem:** Families with multiple children can't visually distinguish which events belong to which child on the calendar. The Child model has a Color field but it's not used.

**Solution:**
- Use existing Child.Color field (or add if missing)
- Add color picker to child creation/edit
- Display child's color as left border/badge on calendar events
- Add optional "Show all" / filter by child toggle

**Targets:**
- `Components/Pages/Calendar.razor` (add color coding, filter toggle)
- `Components/Pages/Calendar.razor.css` (color styling)
- `Components/Pages/Settings.razor` (add child color picker if not exists)

#### Delegation Prompt (Codex)
```
Add multi-child color coding to the Denly calendar.

CONTEXT:
- Child model (Models/Child.cs) has a Color property (string?)
- Events have optional ChildId linking to a child
- Calendar.razor displays events

REQUIREMENTS:
1. Calendar event display:
   - If event has ChildId and child has Color: show 4px left border in that color
   - If event has no ChildId: no color border (shared event)
   - Events retain their EventType color for the main background

2. Add child filter:
   - Add filter bar above calendar: "All" + one chip per child
   - Chip shows child name with their color as background
   - Selecting a child filters to only their events
   - "All" shows everything (default)

3. Color picker in Settings:
   - If child color editing doesn't exist, add it
   - Show color swatches (use existing app palette)
   - Save color to child record

CONSTRAINTS:
- Child.Color already exists - do NOT modify the model
- Use CSS variables from existing app theme where possible
- Do NOT modify Calendar's core event rendering logic beyond adding border
- Keep filter state local (doesn't need to persist)

DELIVERABLES:
- Updated Calendar.razor
- Updated Calendar.razor.css
- Updated Settings.razor if needed
- Build must pass
```

#### Review Checklist
- [ ] Events show child color border
- [ ] Filter chips render correctly
- [ ] Filter works (shows only selected child's events)
- [ ] "All" shows all events
- [ ] Color picker saves correctly
- [ ] Shared events (no child) display without color border

#### Completion Report
<!-- Agent fills in when done -->

---

### P2-4: Full Data Export
**Source:** Data portability requirements | **Effort:** Medium | **Risk:** Low

> **Delegate:** Gemini | **Status:** Ready
> **Reason:** Data aggregation task following established patterns

**Problem:** Users should be able to export all their data at any time (GDPR-style portability). This builds trust and reduces lock-in concerns.

**Solution:**
- Create comprehensive export including all user data
- Export as ZIP containing JSON files per data type
- Include: profile, children, events, expenses, settlements, documents metadata
- Add "Export My Data" button in Settings

**Targets:**
- `Services/IDataExportService.cs` (new)
- `Services/DataExportService.cs` (new)
- `Components/Pages/Settings.razor` (add export button)

#### Delegation Prompt (Gemini)
```
Add full data export to Denly, a .NET MAUI Blazor Hybrid co-parenting app.

CONTEXT:
- User data spans multiple tables: profiles, children, events, expenses, settlements, documents
- Services exist for each data type
- Goal: GDPR-style data portability

REQUIREMENTS:
1. Create IDataExportService:
   - ExportAllDataAsync() -> Task<byte[]> (returns ZIP file bytes)

2. Implement DataExportService:
   - Inject all relevant services (IAuthService, IDenService, IScheduleService, IExpenseService, IDocumentService)
   - Gather all data for current user's den
   - Create ZIP with structure:
     ```
     denly-export-{date}/
       profile.json
       children.json
       events.json
       expenses.json
       settlements.json
       documents.json (metadata only, not actual files)
       README.txt (explains the export)
     ```
   - Use System.IO.Compression for ZIP
   - Format JSON with indentation for readability

3. Add to Settings.razor:
   - "Data Export" section
   - "Export All My Data" button
   - Loading state while generating
   - Use platform share sheet to save/share ZIP

CONSTRAINTS:
- Do NOT include actual document files (storage costs), only metadata
- Do NOT include other den member's private data
- Include data created_by current user OR shared with their den
- Use structured logging
- Handle large data sets gracefully (stream if needed)

DELIVERABLES:
- IDataExportService.cs
- DataExportService.cs
- Updated Settings.razor
- Build must pass
```

#### Review Checklist
- [ ] ZIP contains all expected files
- [ ] JSON is valid and readable
- [ ] README explains the data
- [ ] No other user's private data included
- [ ] Export works for large data sets
- [ ] Share sheet works on iOS and Android

#### Completion Report
<!-- Agent fills in when done -->

---

### P2-5: Daily Agenda Email
**Source:** Cozi competitive analysis | **Effort:** Low | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready
> **Reason:** Simple email templating with existing event data

**Problem:** Users forget upcoming events without checking the app daily. Cozi's daily digest is a beloved feature that many users cite as essential. When users switch away from Cozi, this is often what they miss most.

**Solution:**
- Create email service for sending daily digest
- Send at configurable time (default 7am local)
- Include: today's events, handoffs, reminders
- Opt-in setting in user preferences
- Use Supabase Edge Functions or similar for scheduled sends

**Targets:**
- `Services/IEmailService.cs` (new)
- `Services/EmailService.cs` (new)
- `Components/Pages/Settings.razor` (add email preferences)

#### Delegation Prompt (Codex)
```
Add daily agenda email to Denly, a .NET MAUI Blazor Hybrid co-parenting app.

CONTEXT:
- Events stored in Supabase (see Models/Event.cs, Services/IScheduleService.cs)
- User profiles have email addresses via Supabase Auth
- App uses existing service injection patterns

REQUIREMENTS:
1. Create IEmailService interface:
   - SendDailyAgendaAsync(string userId, DateTime date) -> Task<bool>
   - GetEmailPreferencesAsync(string userId) -> Task<EmailPreferences>
   - UpdateEmailPreferencesAsync(string userId, EmailPreferences prefs) -> Task<bool>

2. Create EmailPreferences model:
   - DailyAgendaEnabled (bool, default false)
   - DailyAgendaTime (TimeSpan, default 07:00)
   - Timezone (string, default UTC)

3. Implement EmailService:
   - Inject IScheduleService to get day's events
   - Format email as clean HTML with:
     - Greeting with date
     - List of today's events (time, title, type)
     - Highlight handoffs prominently
     - Link to open app
   - Use Supabase Edge Functions for scheduling (document setup in completion report)
   - For MVP: Can stub actual email sending, focus on email content generation

4. Update Settings.razor:
   - Add "Email Preferences" section
   - Toggle: "Send daily agenda email"
   - Time picker: "Send at" (dropdown: 6am, 7am, 8am, 9am)
   - Show current user's email (read-only, from auth)

CONSTRAINTS:
- Do NOT implement actual email sending in the .NET app (use Edge Functions)
- Do NOT store email addresses (use auth service)
- Opt-in only, never opt-out
- Use structured logging (ILogger)
- Email content must not include sensitive child details in subject line

DELIVERABLES:
- Services/IEmailService.cs (new)
- Services/EmailService.cs (new)
- Models/EmailPreferences.cs (new)
- Updated Settings.razor
- README section or completion report notes on Edge Function setup
- Build must pass
```

#### Review Checklist
- [ ] Email preferences save correctly
- [ ] Email content includes all day's events
- [ ] Handoffs are highlighted
- [ ] Time picker works correctly
- [ ] Settings UI matches existing style
- [ ] Edge Function setup documented
- [ ] No PII in email subject lines

#### Completion Report
<!-- Agent fills in when done -->

---

### P2-6: Per-Person Calendar Colors
**Source:** Cozi competitive analysis | **Effort:** Low | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready
> **Reason:** UI enhancement, builds on existing color infrastructure

**Problem:** In multi-parent households, it's hard to see at a glance who created an event or who it's assigned to. Cozi assigns colors to each family member for quick visual identification.

**Note:** This is distinct from P2-3 (multi-child color coding). P2-3 adds child colors to events based on which child the event is for. P2-6 adds colors to family members (co-parents, viewers) for filtering and display purposes.

**Solution:**
- Add Color field to Profile/DenMember if not already present
- Show creator's color as indicator on calendar events
- Add color picker in Settings for user's own color
- Allow filtering calendar by family member

**Targets:**
- `Models/Profile.cs` or `Models/DenMember.cs` (add Color field if needed)
- `Components/Pages/Calendar.razor` (show person color indicator)
- `Components/Pages/Settings.razor` (add personal color picker)

#### Delegation Prompt (Codex)
```
Add per-person calendar colors to Denly, a .NET MAUI Blazor Hybrid co-parenting app.

CONTEXT:
- DenMember model exists (see Models/DenMember.cs)
- Events have CreatedBy field linking to user
- Calendar.razor displays events
- P2-3 handles child colors on events - this is DIFFERENT (family member colors)

REQUIREMENTS:
1. Add to DenMember or Profile model:
   - Color (string?) - hex color code
   - Ensure existing records handle null gracefully

2. Calendar event display:
   - Show small colored dot/indicator for event creator
   - Position: top-right corner or beside event title
   - Use creator's DenMember.Color
   - If no color set: use default gray

3. Add member filter:
   - Add filter bar (can coexist with child filter from P2-3)
   - Show "All" + chip per den member
   - Each chip shows member name with their color
   - Filtering shows only events created by that member
   - Combine with child filter if both active

4. Settings color picker:
   - Add "My Color" section in Settings
   - Show color swatches (8-10 preset colors from app palette)
   - Preview of how color appears
   - Save to current user's profile/den member record

CONSTRAINTS:
- Do NOT modify event creation (colors are per-person, not per-event)
- Do NOT conflict with P2-3 child colors (these are separate concepts)
- Maintain visual clarity (person indicator should be subtle, not overwhelming)
- Use structured logging
- Keep filter state local (doesn't need to persist)

DATABASE NOTE:
May need to add `color` column to profiles or den_members table. Document in completion report.

DELIVERABLES:
- Updated Models/DenMember.cs or Models/Profile.cs
- Updated Calendar.razor
- Updated Calendar.razor.css
- Updated Settings.razor
- Build must pass
```

#### Review Checklist
- [ ] Color picker saves to profile correctly
- [ ] Events show creator color indicator
- [ ] Member filter chips display correctly
- [ ] Filter works (shows only selected member's events)
- [ ] Combines properly with child filter (if P2-3 implemented)
- [ ] Color indicator is subtle, not distracting
- [ ] Default color for members without color set

#### Completion Report
<!-- Agent fills in when done -->

---

### P2-11: Calendar "Viewed" Tracking
**Source:** OFW gap identified in reviews | **Effort:** Low | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready
> **Reason:** Simple model extension and UI update, isolated change

**Problem:** When one co-parent adds or modifies a calendar event, they have no way to know if the other parent has seen it. This leads to "I didn't know about that" conflicts, especially for handoff changes.

**Solution:**
- Track when each den member views an event
- Show "viewed by" indicator on events
- Highlight unviewed events for each user
- Optional notification when co-parent views important events

**Targets:**
- `Models/EventView.cs` (new - tracks who viewed what)
- `Services/IScheduleService.cs` (add view tracking methods)
- `Services/SupabaseScheduleService.cs` (implement view tracking)
- `Components/Pages/Calendar.razor` (show viewed indicators, highlight unviewed)

#### Delegation Prompt (Codex)
```
Add calendar event "viewed" tracking to Denly.

CONTEXT:
- Events stored in Supabase (see Models/Event.cs)
- Event has: Id, DenId, Title, StartTime, EndTime, CreatedBy, etc.
- Calendar.razor displays events
- Den has multiple members who need to see each other's events

REQUIREMENTS:
1. Create EventView model:
   - Id (string)
   - EventId (string) - foreign key to Event
   - UserId (string) - who viewed it
   - ViewedAt (DateTime) - when they viewed it

2. Add to IScheduleService:
   - MarkEventViewedAsync(string eventId) -> Task
   - GetEventViewsAsync(string eventId) -> Task<List<EventView>>
   - GetUnviewedEventsAsync() -> Task<List<Event>> (events current user hasn't seen)

3. Implement in SupabaseScheduleService:
   - On event detail view, call MarkEventViewedAsync
   - Creator automatically counts as "viewed"
   - Track views per user per event (don't duplicate)

4. Update Calendar.razor:
   - Show small indicator on events: "Viewed by [co-parent name]" or "Not yet viewed"
   - Use subtle icon (eye icon, checkmark, or similar)
   - Highlight unviewed events (border or background tint)
   - On event tap/expand, automatically mark as viewed
   - Add "Unviewed" filter option

5. Event list display:
   - Show "New" badge on events created by co-parent that current user hasn't viewed
   - After viewing, badge disappears
   - Count of unviewed events in nav/header

CONSTRAINTS:
- Do NOT modify Event model (views are separate table)
- Creator automatically counts as viewed (don't show "not viewed" for your own events)
- Only track views for den members (not viewers from P2-2)
- Use structured logging
- Viewed status should sync across devices

DATABASE NOTE:
Create `event_views` table:
- id (uuid, primary key)
- event_id (uuid, references events)
- user_id (uuid, references profiles)
- viewed_at (timestamptz)
- Unique constraint on (event_id, user_id)

DELIVERABLES:
- Models/EventView.cs (new)
- Updated IScheduleService.cs
- Updated SupabaseScheduleService.cs
- Updated Calendar.razor
- Updated Calendar.razor.css
- Build must pass
```

#### Review Checklist
- [ ] Viewing event marks it as viewed
- [ ] Creator doesn't see "not viewed" on own events
- [ ] Viewed indicator shows on calendar events
- [ ] Unviewed events are highlighted
- [ ] "New" badge appears on unviewed events
- [ ] View status syncs across devices
- [ ] Performance OK with many events

#### Completion Report
<!-- Agent fills in when done -->

---

### Additional P2 Items (Blocked)

| ID | Feature | Description | Rationale | Status |
|----|---------|-------------|-----------|--------|
| P2-10 | Message drafts auto-save | Never lose draft if app closes | OFW complaint: lose large drafts | Blocked (P3-8) |

> **Note:** P2-10 blocked by messaging feature (P3-8). Will be prepared when messaging is implemented.

---

## P3: Future Expansion

### Blocked Items (Require Messaging Feature)

> **Note:** These items were originally P1/P2 but depend on a messaging system that doesn't exist yet. They'll be unblocked when secure messaging is implemented.

| ID | Feature | Description | Blocked By |
|----|---------|-------------|------------|
| P3-B1 | Read receipts | Show when messages were read with timestamp | Messaging not implemented |
| P3-B2 | Trade/Swap requests | Structured schedule change proposals with Yes/No response | Messaging not implemented |
| P3-B3 | Immutable messages | Messages cannot be deleted or edited after sending | Messaging not implemented |
| P3-B4 | Tone suggestions | Flag potentially heated language before sending | Messaging not implemented |

### Future Features

| ID | Feature | Description | Complexity |
|----|---------|-------------|------------|
| P3-1 | Professional access | Lawyer/mediator view-only accounts | High |
| P3-2 | In-app calling | Audio/video calls between family members | High |
| P3-3 | Recurring expense templates | Auto-log regular costs | Low |
| P3-4 | Custody schedule templates | Pre-built patterns (50/50, 2-2-3, etc.) | Medium |
| P3-5 | Family mode | Broader positioning for all families, not just separated | Medium |
| P3-6 | Shared supply lists | "Out of diapers at mom's house" notifications | Low |
| P3-7 | Kid accounts | Age-appropriate access for older children | Medium |
| P3-8 | Secure messaging | End-to-end encrypted messaging between co-parents | High |
| P3-9 | Chore/task assignment | Assign tasks to specific family members with due dates | Medium |
| P3-10 | Meal planning | Weekly meal calendar with optional recipe links | Medium |

---

## Refactor Ideas (Non-Feature)

Low-priority code quality improvements. Not assigned to agents—do opportunistically.

| Area | Idea |
|------|------|
| Services | Extract common Supabase patterns to base class |
| Models | Add data annotations for validation |
| UI | Create shared form components (date picker, dropdown) |
| Testing | Add unit test project, start with service tests |
| Logging | Migrate remaining Console.WriteLine to ILogger |

---

## Agent Suggestions

> **For Codex/Gemini only.** This is the ONLY section you may edit to add new ideas.
> See `docs/AGENTS.md` → "For Codex/Gemini: Suggesting New Items" for format and rules.
>
> **⚠️ DO NOT modify any other section of this file.**

<!--
AGENTS: Append your suggestions below this line using the format from AGENTS.md.
HUMANS/CLAUDE: Review periodically, promote good ideas to proper backlog items, delete implemented/rejected suggestions.
-->

*No suggestions yet.*

### Make member removal X more prominent
- **Suggested by:** Codex
- **Date:** 2026-01-21
- **Context:** Reviewing Den member removal UI visibility
- **Idea:** Increase the visibility of the remove member X button so it is easier to discover and use.
- **Potential files:** Components/Pages/Settings.razor, Components/Pages/Settings.razor.css

### Make Family Vault filters fully visible
- **Suggested by:** Codex
- **Date:** 2026-01-21
- **Context:** Reviewing Family Vault filter discoverability
- **Idea:** Show all filters without horizontal scrolling so the "Other" option is always visible.
- **Potential files:** Components/Pages/FamilyVault.razor, Components/Pages/FamilyVault.razor.css

### Fix scheduler AM/PM saving and edit regression
- **Suggested by:** Codex
- **Date:** 2026-01-21
- **Context:** Reviewing schedule time picker behavior
- **Idea:** Resolve scheduler time selection so AM/PM persists correctly when saving and when editing existing items.
- **Potential files:** Components/Pages/Calendar.razor, Services/IScheduleService.cs, Services/SupabaseScheduleService.cs
