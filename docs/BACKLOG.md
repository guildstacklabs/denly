# Denly Product Backlog

## Quick Reference: Delegation Summary

| ID | Feature | Delegate | Status |
|----|---------|----------|--------|
| P1-1 | Calendar reminders | Claude | Ready |
| P1-2 | Exportable reports | Gemini | Ready |
| P1-3 | Info Bank expansion | Gemini | Ready |
| P2-1 | Settlement confirmation | Codex | Ready |
| P2-2 | Third-party view access | Gemini | Ready |
| P2-3 | Multi-child color coding | Codex | Ready |
| P2-4 | Full data export | Gemini | Ready |

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
