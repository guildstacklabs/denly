# Denly Product Backlog

## Recently Completed

| Feature | Notes |
|---------|-------|
| Home page real expense data | Replaced mock "$218.40" with live `GetBalancesAsync()` |
| Document upload UI | File picker + metadata modal in InfoBank Vault view |
| Child edit button | InfoBank "Edit Info" navigates to Settings |
| Flexible expense splits (P1-7) | 50/50, 60/40, 70/30, 80/20 selector in AddExpenseDialog |
| Settlement confirmation (P2-1) | Recipients confirm payments; status badge on Expenses page |
| Calendar ICS subscription (P2-2) | Supabase Edge Function serves live `.ics` feed; subscribe button in Calendar header |
| Multi-child associations (P2-4) | Junction tables `event_children`/`expense_children`; multi-select checkboxes; filter-by-child on Calendar and Expenses |

---

## MVP Parity (Before Launch)

| # | Feature | Effort | Cost | Notes |
|---|---------|--------|------|-------|
| 1 | Push notifications (APNs) | High | $99/yr Apple Dev | Device tokens stored; Edge Function exists but APNs incomplete |
| 3 | Local calendar reminders | High | None | Platform-specific (iOS UNUserNotificationCenter, Android AlarmManager) |

---

## Post-Launch

| # | Feature | Effort | Notes |
|---|---------|--------|-------|
| 5 | Data export (GDPR) | High | ZIP with JSON files per data type |
| 6 | Third-party view access | High | Read-only Viewer role for grandparents/nannies |
| 7 | Daily digest email | High | Supabase Edge Function, opt-in preference |
| 8 | Calendar "viewed" tracking | Medium | Show if co-parent saw schedule changes |
| 9 | Multi-child color coding | Medium | Child.Color on event borders, filter chips |
| 10 | Per-person calendar colors | Medium | Creator color indicator on events |
| 11 | PDF report summary stats | Low | Totals, splits, balances in expense PDF |

---

## Deferred (Wrong Market Fit)

These don't fit the "amicable co-parents" target market:

- Secure messaging (users have existing messaging apps)
- ToneMeter AI (high-conflict feature)
- GPS check-ins (privacy/legal complexity)
- Professional access (Phase 2+)
- Meal planning / to-do lists (scope creep)

---

## Code Improvements

| # | Issue | Severity | File(s) |
|---|-------|----------|---------|
| C1 | ~~Insecure invite code generation (`Random` → crypto RNG)~~ | Critical | `SupabaseInviteService.cs` |
| C2 | ~~Broken auth timeout (CTS applied to wrong task)~~ | Critical | `SupabaseAuthService.cs` |
| C3 | ~~Silent exception swallowing in session restore/persist~~ | Critical | `SupabaseAuthService.cs` |
| C4 | ~~Crash in `RemoveMemberAsync` (`.Single()` → `.SingleOrDefault()`)~~ | Critical | `SupabaseDenService.cs` |
| C5 | ~~Inconsistent DateTime formatting in Supabase queries~~ | High | `SupabaseScheduleService.cs` |
| C6 | ~~BalanceCalculator silent fallback on zero splits~~ | High | `BalanceCalculator.cs` |
| C7 | ~~Empty `Dispose()` in MainLayout~~ | Low | `MainLayout.razor` |
| C8 | Duplicate `GetInitials()` logic | Medium | `Expenses.razor`, `Settings.razor` |
| C9 | CSS duplication across modal components | Medium | Multiple `.razor.css` files |
| C10 | Calendar O(n×42) performance in `BuildCalendarDays()` | Medium | `Calendar.razor` |
| C11 | Serial awaits in `Settings.LoadData()` | Medium | `Settings.razor` |
| C12 | Missing input length limits on forms | Low | Calendar, Expenses, InfoBank |
| C13 | Accessibility: missing `aria-label`, `role="alert"` | Low | Multiple components |
| C14 | CSS inconsistencies (border-radius, padding, font-size) | Low | Multiple `.razor.css` |

---

## Pre-Release Checklist

### iOS Push Notifications Setup

**Status:** Not started

1. **Apple Developer Account** — Create App ID with Push Notifications capability; generate APNs key (p8 file)
2. **Provisioning Profile** — Create profile with Push Notifications entitlement
3. **Supabase Configuration** — Upload APNs key; configure Edge Function
4. **Build Configuration** — `Platforms/iOS/Entitlements.Release.plist` has push entitlements (Release builds only)
