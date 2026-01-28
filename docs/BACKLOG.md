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

## Pre-Release Checklist

### iOS Push Notifications Setup

**Status:** Not started

1. **Apple Developer Account** — Create App ID with Push Notifications capability; generate APNs key (p8 file)
2. **Provisioning Profile** — Create profile with Push Notifications entitlement
3. **Supabase Configuration** — Upload APNs key; configure Edge Function
4. **Build Configuration** — `Platforms/iOS/Entitlements.Release.plist` has push entitlements (Release builds only)
