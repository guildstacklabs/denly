# Denly Product Backlog

See `BACKLOG_ARCHIVE.md` for completed items. See `AGENTS.md` for workflow.

## Active Work

Ordered by **urgency** (critical first), then **complexity** (simple first within each tier).

| ID | Feature | Urgency | Complexity | Delegate | Status |
|----|---------|---------|------------|----------|--------|
| P0-6 | Push notifications | Critical | High | Claude | Ready |
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

---

## Blocked

| ID | Feature | Blocked By | Notes |
|----|---------|------------|-------|
| P1-9 | Scheduled messages | P3-8 | Needs secure messaging infrastructure |
| P2-10 | Message drafts auto-save | P3-8 | Needs messaging UI to auto-save into |

---

## Item Details

### P2-7: Member Removal X Visibility
**Problem:** Remove member button hard to discover.
**Solution:** Improve visibility with coral color, 44px touch target, ARIA label.
**Review:** Visible on desktop/mobile, hover state works, accessible.

### P2-8: Family Vault Filter Visibility
**Problem:** Filters require horizontal scroll, hiding some options.
**Solution:** Wrap filters to multiple rows (flex-wrap).
**Review:** All filters visible without scrolling, responsive.

### P2-12: PDF Report Summary Statistics
**Problem:** PDF expense reports lack summary statistics (totals, splits, balances).
**Solution:** Add summary section to PDF with total expenses, per-person totals, and net balance.
**Review:** PDF shows total, each parent's share, net owed. Numbers match app calculations.

### P1-7: Flexible Expense Splits
**Problem:** Only 50/50 splits supported; real arrangements vary (60/40, 70/30).
**Solution:** Add DefaultSplitPercent to Den, SplitPercent to Expense, update balance calculations.
**Review:** Default split saves, per-expense override works, null defaults to 50%.

### P2-1: Settlement Confirmation
**Problem:** No confirmation when settlement payment received.
**Solution:** Add ConfirmedAt/ConfirmedBy to Settlement, "Confirm Receipt" button for recipient.
**Review:** Only recipient sees confirm button, badge styling matches theme.

### P2-3: Multi-Child Color Coding
**Problem:** Can't distinguish which events belong to which child.
**Solution:** Use Child.Color for event border, add filter chips by child.
**Review:** Color border shows, filter works, shared events have no border.

### P2-6: Per-Person Calendar Colors
**Problem:** Can't see at a glance who created an event.
**Solution:** Add Color to DenMember, show creator color indicator on events.
**Review:** Color picker saves, indicator subtle, filter by member works.

### P2-11: Calendar "Viewed" Tracking
**Problem:** No way to know if co-parent saw calendar changes.
**Solution:** Track views per event, show "viewed by" indicator, highlight unviewed.
**Review:** Viewing marks as viewed, creator auto-viewed, syncs across devices.

### P2-2: Third-Party View Access
**Problem:** Grandparents/nannies need calendar access without full app access.
**Solution:** Add Viewer role to DenMember, calendar read-only, hide other nav items.
**Review:** Viewers can't access Expenses/Vault, can't edit events, role persists.

### P2-4: Full Data Export
**Problem:** Users need GDPR-style data portability.
**Solution:** ZIP export with JSON files (profile, children, events, expenses, etc.).
**Review:** ZIP complete, JSON valid, no other user's private data.

### P2-5: Daily Agenda Email
**Problem:** Users miss events without checking app daily.
**Solution:** Opt-in daily digest via Supabase Edge Functions.
**Review:** Preferences save, handoffs highlighted, no PII in subject.

### P1-8: Calendar Sync
**Problem:** Users want events in Google/Apple Calendar without manual entry.
**Solution:** Export/import .ics files via `ICalendarSyncService`.
**Review:** .ics opens in Google/Apple Calendar, times correct, UID stable.

### P1-1: Calendar Reminders
**Problem:** No reminder system for handoffs/appointments.
**Solution:** `INotificationService` with platform-specific implementations (iOS: UNUserNotificationCenter, Android: AlarmManager).
**Review:** Permission on user action only, reminders survive restart, no PII in notifications.

### P0-6: Push Notifications
**Problem:** Users miss important updates (new expenses, calendar changes, settlements) when not actively using the app.
**Solution:**
- Create `IPushNotificationService` interface with platform implementations
- iOS: Register with APNs via UNUserNotificationCenter
- Android: Register with FCM via FirebaseMessaging
- Backend: Supabase Edge Function triggered by database changes, sends via FCM/APNs
- Store device tokens in `device_tokens` table (user_id, platform, token, created_at)
- Notification types: expense_added, event_created, event_updated, settlement_requested, settlement_confirmed
**Targets:**
- `Services/IPushNotificationService.cs` (interface)
- `Services/PushNotificationService.cs` (shared logic)
- `Platforms/iOS/PushNotificationHandler.cs`
- `Platforms/Android/PushNotificationHandler.cs`
- `supabase/functions/push-notify/index.ts` (Edge Function)
**Review:**
- Permission requested only on user action (not app launch)
- Token refresh handled gracefully
- No PII in notification payload (use IDs, fetch details in-app)
- Notifications appear when app backgrounded
- Tapping notification opens relevant screen
- Works offline (queued server-side)

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

## Agent Suggestions

> Codex/Gemini: Append suggestions here only. See `AGENTS.md` for format.

*No suggestions yet.*
