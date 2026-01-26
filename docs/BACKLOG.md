# Denly Product Backlog

See `BACKLOG_ARCHIVE.md` for completed items. See `AGENTS.md` for workflow.

## Active Work

Ordered by **urgency** (critical first), then **complexity** (simple first within each tier).

| ID | Feature | Urgency | Complexity | Delegate | Status |
|----|---------|---------|------------|----------|--------|
| P1-7 | Flexible expense splits | High | Medium | Codex | Ready |
| P1-8 | Calendar sync (.ics) | High | High | Claude | Ready |
| P1-1 | Calendar reminders | High | High | Claude | Ready |
| P2-1 | Settlement confirmation | Medium | Medium | Codex | Ready |
| P2-11 | Calendar "viewed" tracking | Medium | Medium | Codex | Ready |
| P2-4 | Full data export | Medium | High | Gemini | Ready |
| P2-2 | Third-party view access | Medium | High | Gemini | Ready |
| P2-12 | PDF report summary stats | Low | Low | Codex | Ready |
| P2-7 | Member removal X visibility | Low | Low | Codex | Ready |
| P2-8 | Family Vault filter visibility | Low | Low | Codex | Ready |
| P2-3 | Multi-child color coding | Low | Medium | Codex | Ready |
| P2-6 | Per-person calendar colors | Low | Medium | Codex | Ready |
| P2-5 | Daily agenda email | Low | High | Gemini | Ready |
| P1-10 | Child associations for items | High | Medium | Claude | Ready |

---

## Blocked

| ID | Feature | Blocked By | Notes |
|----|---------|------------|-------|
| P1-9 | Scheduled messages | P3-8 | Needs secure messaging infrastructure |
| P2-10 | Message drafts auto-save | P3-8 | Needs messaging UI to auto-save into |

---

## Item Details

### P1-7: Flexible Expense Splits
**Source:** Feature request | **Effort:** Medium | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready

**Problem:** Only 50/50 splits supported; real arrangements vary (60/40, 70/30).

**Solution:**
- Add `DefaultSplitPercent` column to Den table
- Add `SplitPercent` column to Expense table
- Update balance calculations to use split percentage
- Null defaults to 50%

**Targets:**
- `supabase/migrations/` (new migration)
- `Models/Den.cs`
- `Models/Expense.cs`
- `Services/ExpenseService.cs`
- `Components/Pages/AddExpense.razor`
- `Components/Pages/DenSettings.razor`

#### Review Checklist
- [ ] Default split saves to Den
- [ ] Per-expense override works
- [ ] Null defaults to 50%
- [ ] Balance calculations correct
- [ ] `dotnet build` passes
- [ ] `dotnet test` passes

#### Completion Report
<!-- Agent fills this in when done -->

---

### P1-8: Calendar Sync
**Source:** Feature request | **Effort:** High | **Risk:** Low

> **Delegate:** Claude | **Status:** Ready
> **Reason:** External calendar integration requires careful handling of timezones and UID stability

**Problem:** Users want events in Google/Apple Calendar without manual entry.

**Solution:**
- Create `ICalendarSyncService` interface
- Export .ics files for download
- Import .ics files to create events
- Stable UIDs for update support

**Targets:**
- `Services/ICalendarSyncService.cs` (new interface)
- `Services/CalendarSyncService.cs` (new service)
- `Components/Pages/Calendar.razor`
- `Components/Shared/CalendarSyncModal.razor`

#### Review Checklist
- [ ] .ics file opens in Google Calendar
- [ ] .ics file opens in Apple Calendar
- [ ] Times are correct across timezones
- [ ] UID is stable for updates
- [ ] Import creates events correctly
- [ ] `dotnet build` passes
- [ ] `dotnet test` passes

#### Completion Report
<!-- Agent fills this in when done -->

---

### P1-1: Calendar Reminders
**Source:** Core feature | **Effort:** High | **Risk:** Medium

> **Delegate:** Claude | **Status:** Ready
> **Reason:** Platform-specific implementations (iOS/Android) require careful architecture

**Problem:** No reminder system for handoffs/appointments.

**Solution:**
- Create `INotificationService` interface
- iOS: UNUserNotificationCenter implementation
- Android: AlarmManager implementation
- Schedule reminders per event preference

**Targets:**
- `Services/INotificationService.cs` (new interface)
- `Platforms/iOS/NotificationService.cs`
- `Platforms/Android/NotificationService.cs`
- `Components/Pages/AddEvent.razor`
- `Components/Pages/EditEvent.razor`

#### Review Checklist
- [ ] Permission requested only on user action (not app launch)
- [ ] Reminders survive app restart
- [ ] Reminders survive device restart
- [ ] No PII in notification text
- [ ] Reminder time configurable per event
- [ ] `dotnet build` passes
- [ ] `dotnet test` passes

#### Completion Report
<!-- Agent fills this in when done -->

---

### P1-10: Child Associations for Items
**Source:** Core feature | **Effort:** Medium | **Risk:** Medium

> **Delegate:** Claude | **Status:** Ready
> **Reason:** Data model change (junction tables) requires migration planning and RLS updates

**Problem:** Users can't associate children with calendar events, expenses, or documents in the UI.

**Solution:**
- Add child multi-select picker to Event, Expense, and Document create/edit forms
- Multi-select allows single child, multiple children, or none (= all children)
- No selection = "All children" (den-wide item). Fast path for quick entry.
- Selected children display as chips/tags with their assigned colors
- Display associated child name(s)/color(s) on item cards
- Add filter-by-child capability to each feature

**Data model:** Replace `child_id` (single FK) with junction tables: `event_children`, `expense_children`, `document_children`. Proper FKs, clean RLS, easy filtering.

**Targets:**
- `supabase/migrations/` (junction tables migration)
- `Models/EventChild.cs` (new model)
- `Models/ExpenseChild.cs` (new model)
- `Models/DocumentChild.cs` (new model)
- `Services/IEventService.cs`
- `Services/EventService.cs`
- `Services/IExpenseService.cs`
- `Services/ExpenseService.cs`
- `Services/IDocumentService.cs`
- `Services/DocumentService.cs`
- `Components/Shared/ChildPicker.razor` (new component)
- `Components/Pages/AddEvent.razor`
- `Components/Pages/AddExpense.razor`
- `Components/Pages/AddDocument.razor`

#### Review Checklist
- [ ] Child picker appears on event form
- [ ] Child picker appears on expense form
- [ ] Child picker appears on document form
- [ ] Multi-select works correctly
- [ ] Empty selection displays as "All children"
- [ ] Chips show child colors
- [ ] Filter by child works on calendar
- [ ] Filter by child works on expenses
- [ ] Filter by child works on vault
- [ ] `dotnet build` passes
- [ ] `dotnet test` passes

#### Completion Report
<!-- Agent fills this in when done -->

---

### P2-1: Settlement Confirmation
**Source:** Feature request | **Effort:** Medium | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready

**Problem:** No confirmation when settlement payment received.

**Solution:**
- Add `ConfirmedAt` and `ConfirmedBy` columns to Settlement table
- Add "Confirm Receipt" button visible only to recipient

**Targets:**
- `supabase/migrations/` (new migration)
- `Models/Settlement.cs`
- `Services/SettlementService.cs`
- `Components/Pages/Settlements.razor`
- `Components/Pages/Settlements.razor.css`

#### Review Checklist
- [ ] Only recipient sees confirm button
- [ ] Confirmation saves timestamp and user
- [ ] Badge styling matches theme
- [ ] `dotnet build` passes
- [ ] `dotnet test` passes

#### Completion Report
<!-- Agent fills this in when done -->

---

### P2-11: Calendar "Viewed" Tracking
**Source:** Feature request | **Effort:** Medium | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready

**Problem:** No way to know if co-parent saw calendar changes.

**Solution:**
- Create `event_views` junction table
- Track views per event per user
- Show "viewed by" indicator on events
- Highlight unviewed events

**Targets:**
- `supabase/migrations/` (new migration)
- `Models/EventView.cs`
- `Services/IEventService.cs`
- `Services/EventService.cs`
- `Components/Pages/Calendar.razor`
- `Components/Shared/EventCard.razor`

#### Review Checklist
- [ ] Viewing marks event as viewed
- [ ] Creator is auto-viewed
- [ ] View status syncs across devices
- [ ] Unviewed events highlighted
- [ ] `dotnet build` passes
- [ ] `dotnet test` passes

#### Completion Report
<!-- Agent fills this in when done -->

---

### P2-4: Full Data Export
**Source:** GDPR compliance | **Effort:** High | **Risk:** Low

> **Delegate:** Gemini | **Status:** Ready

**Problem:** Users need GDPR-style data portability.

**Solution:**
- Create ZIP export with JSON files
- Include: profile, children, events, expenses, documents metadata, settlements

**Targets:**
- `Services/IDataExportService.cs` (new interface)
- `Services/DataExportService.cs` (new service)
- `Components/Pages/Profile.razor`

#### Review Checklist
- [ ] ZIP file downloads successfully
- [ ] JSON files are valid
- [ ] No other user's private data included
- [ ] All user's data types included
- [ ] `dotnet build` passes
- [ ] `dotnet test` passes

#### Completion Report
<!-- Agent fills this in when done -->

---

### P2-2: Third-Party View Access
**Source:** Feature request | **Effort:** High | **Risk:** Medium

> **Delegate:** Gemini | **Status:** Ready

**Problem:** Grandparents/nannies need calendar access without full app access.

**Solution:**
- Add `Viewer` role to DenMember enum
- Viewers get calendar read-only access
- Hide Expenses/Vault nav items for viewers

**Targets:**
- `Models/DenMember.cs` (role enum)
- `Services/DenMemberService.cs`
- `Components/Shared/NavMenu.razor`
- `Components/Pages/Calendar.razor`
- `Components/Pages/Invite.razor`

#### Review Checklist
- [ ] Viewers cannot access Expenses page
- [ ] Viewers cannot access Family Vault
- [ ] Viewers cannot edit events
- [ ] Role persists across sessions
- [ ] Invite flow supports Viewer role
- [ ] `dotnet build` passes
- [ ] `dotnet test` passes

#### Completion Report
<!-- Agent fills this in when done -->

---

### P2-12: PDF Report Summary Statistics
**Source:** Feature request | **Effort:** Low | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready

**Problem:** PDF expense reports lack summary statistics (totals, splits, balances).

**Solution:**
- Add summary section to PDF with total expenses
- Include per-person totals and net balance

**Targets:**
- `Services/PdfReportService.cs`

#### Review Checklist
- [ ] PDF shows total expenses
- [ ] PDF shows each parent's share
- [ ] PDF shows net owed amount
- [ ] Numbers match app calculations
- [ ] `dotnet build` passes

#### Completion Report
<!-- Agent fills this in when done -->

---

### P2-7: Member Removal X Visibility
**Source:** UX review | **Effort:** Low | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready

**Problem:** Remove member button hard to discover.

**Solution:**
- Improve visibility with coral color
- Ensure 44px touch target
- Add ARIA label for accessibility

**Targets:**
- `Components/Pages/DenMembers.razor`
- `Components/Pages/DenMembers.razor.css`

#### Review Checklist
- [ ] Button visible on desktop and mobile
- [ ] Hover state works correctly
- [ ] Touch target is 44px minimum
- [ ] ARIA label present
- [ ] `dotnet build` passes

#### Completion Report
<!-- Agent fills this in when done -->

---

### P2-8: Family Vault Filter Visibility
**Source:** UX review | **Effort:** Low | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready

**Problem:** Filters require horizontal scroll, hiding some options.

**Solution:**
- Apply flex-wrap to filter container
- Ensure filters wrap to multiple rows on narrow screens

**Targets:**
- `Components/Pages/FamilyVault.razor.css`

#### Review Checklist
- [ ] All filters visible without horizontal scrolling
- [ ] Layout responsive on mobile
- [ ] `dotnet build` passes

#### Completion Report
<!-- Agent fills this in when done -->

---

### P2-3: Multi-Child Color Coding
**Source:** Feature request | **Effort:** Medium | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready

**Problem:** Can't distinguish which events belong to which child.

**Solution:**
- Use `Child.Color` for event border styling
- Add filter chips by child on calendar

**Targets:**
- `Components/Pages/Calendar.razor`
- `Components/Pages/Calendar.razor.css`
- `Components/Shared/EventCard.razor`
- `Components/Shared/EventCard.razor.css`

#### Review Checklist
- [ ] Color border shows on child-specific events
- [ ] Filter by child works
- [ ] Shared events (no child) have no color border
- [ ] `dotnet build` passes

#### Completion Report
<!-- Agent fills this in when done -->

---

### P2-6: Per-Person Calendar Colors
**Source:** Feature request | **Effort:** Medium | **Risk:** Low

> **Delegate:** Codex | **Status:** Ready

**Problem:** Can't see at a glance who created an event.

**Solution:**
- Add `Color` column to DenMember table
- Show creator color indicator on events
- Add filter by member

**Targets:**
- `supabase/migrations/` (new migration)
- `Models/DenMember.cs`
- `Services/DenMemberService.cs`
- `Components/Pages/Calendar.razor`
- `Components/Shared/EventCard.razor`
- `Components/Shared/ColorPicker.razor`

#### Review Checklist
- [ ] Color picker saves to DenMember
- [ ] Creator indicator is subtle but visible
- [ ] Filter by member works
- [ ] `dotnet build` passes
- [ ] `dotnet test` passes

#### Completion Report
<!-- Agent fills this in when done -->

---

### P2-5: Daily Agenda Email
**Source:** Feature request | **Effort:** High | **Risk:** Medium

> **Delegate:** Gemini | **Status:** Ready

**Problem:** Users miss events without checking app daily.

**Solution:**
- Opt-in daily digest preference
- Supabase Edge Function sends morning email
- Highlight handoffs and appointments

**Targets:**
- `supabase/migrations/` (user preferences)
- `supabase/functions/daily-digest/index.ts` (new Edge Function)
- `Models/UserPreferences.cs`
- `Services/UserPreferencesService.cs`
- `Components/Pages/Profile.razor`

#### Review Checklist
- [ ] Preference saves correctly
- [ ] Edge Function triggers daily
- [ ] Handoffs highlighted in email
- [ ] No PII in email subject line
- [ ] Unsubscribe link works
- [ ] `dotnet build` passes

#### Completion Report
<!-- Agent fills this in when done -->

---

## P3: Future Expansion

| ID | Feature | Complexity |
|----|---------|------------|
| P3-1 | Professional access (lawyers) | High |
| P3-2 | In-app calling | High |
| P3-3 | Recurring expense templates | Low |
| P3-4 | Custody schedule templates | Medium |
| P3-5 | Family mode (not just co-parents) | Medium |
| P3-6 | Shared supply lists | Low |
| P3-7 | Kid accounts | Medium |
| P3-8 | Secure messaging | High |
| P3-9 | Chore/task assignment | Medium |
| P3-10 | Meal planning | Medium |

**Blocked by P3-8 (messaging):** Read receipts, Trade/Swap requests, Immutable messages, Tone suggestions

---

## Refactor Ideas

| Area | Idea |
|------|------|
| Services | Extract common Supabase patterns to base class |
| Models | Add data annotations for validation |
| UI | Create shared form components (date picker, dropdown) |
| Logging | Migrate remaining Console.WriteLine to ILogger |

---

## Pre-Release Checklist

Tasks required before deploying to real devices or app stores.

### iOS Push Notifications Setup

**Status:** Not started

Before testing push notifications on real iOS devices:

1. **Apple Developer Account**
   - Create App ID with Push Notifications capability at [developer.apple.com](https://developer.apple.com)
   - Generate APNs authentication key (p8 file) or certificates

2. **Provisioning Profile**
   - Create provisioning profile with Push Notifications entitlement
   - Download and install in Xcode/Keychain

3. **Supabase Configuration**
   - Upload APNs key to Supabase or configure your push provider
   - Set up Edge Function to send push notifications

4. **Build Configuration**
   - The `Platforms/iOS/Entitlements.Release.plist` file contains push entitlements
   - These are only applied for Release builds (Debug/Simulator skips them)
   - Build with `dotnet build -c Release -f net10.0-ios` for device deployment

**Why this matters:** Push notification entitlements require valid code signing certificates. During development, we disabled entitlements for Debug builds to allow simulator testing. Release builds will include the entitlements and require proper certificates.

---

## Agent Suggestions

> Codex/Gemini: Append suggestions here only. See `AGENTS.md` for format.

### Suggestion: Accessibility Preferences Menu
**Source:** Codex | **Effort:** Medium | **Risk:** Low

**Problem:** Accessibility tweaks (contrast, focus rings, tap target sizing) should be user-selectable rather than global, to preserve the minimalist visual system by default.

**Solution:**
- Add an “Accessibility” section in Settings with toggles for high-contrast text, focus ring visibility, and larger tap targets.
- Store preferences in user settings and apply via data attributes on the root container (e.g., `data-a11y-contrast`, `data-a11y-focus`, `data-a11y-tap`).
- Update CSS tokens and selectors to respond to these attributes without changing default visuals.

**Targets:**
- `Components/Pages/Settings.razor`
- `Components/Pages/Settings.razor.css`
- `wwwroot/app.css`
- `Services/IUserPreferencesService.cs` (new)
- `Services/UserPreferencesService.cs` (new)
