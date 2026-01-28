# CLAUDE.md

## Project: Denly

A co-parenting coordination app built by GuildStack Labs. Competes with Cozi and OurFamilyWizard, targeting **amicable co-parents** who don't need court-admissible features.

## Mission

Fair-priced, community-driven alternative to expensive co-parenting apps like OurFamilyWizard ($100-200/year). Pay-what-you-want model ($0-15/year suggested).

## Tech Stack

- .NET MAUI Blazor Hybrid (.NET 10)
- Target platforms: iOS and Android
- Backend: Supabase (PostgreSQL + Auth + Storage)
- Auth: Supabase GoTrue (email/password + Google OAuth)
- Storage: Supabase Storage (receipts bucket)

## MVP Features (3 pillars)

1. **Shared Calendar** — Custody schedule, appointments (month/week/agenda views, timezone support)
2. **Expense Tracker** — Log, split (50/50 to 80/20), settle, confirm payments
3. **Document Storage** — Upload, categorize (medical/school/legal/identity), browse by folder

## Core Values

- **Security-first** — Protecting family data
- **Radical cost transparency**
- **Open source**
- **No profit extraction** — Sustainability only

## Architecture

- **"Den"** = family unit. Each den has members with roles: `owner`, `co-parent`, `observer`
- **Invite system** — 8-character codes, 3-day expiry, rate-limited (5 failed attempts = lockout)
- **Services** use Supabase client via `SupabaseServiceBase` with DI
- **Caching** — In-memory 5-min TTL for members, profiles, balances
- **Balance calculation** — `BalanceCalculator.cs` supports per-expense custom splits via `expense_splits` table

## Key Patterns

- Razor components in `Components/Pages/` and `Components/Shared/`
- Scoped CSS per component (`.razor.css` files)
- Modal dialogs follow the pattern: `IsOpen` parameter, overlay + content div, `OnCancel`/`OnSave` callbacks
- File picker uses `FilePicker.Default.PickAsync()` (MAUI native)
- Design system uses CSS variables: `--color-surface-glass`, `--color-border-soft`, `--shadow-pebble`, etc.
- Navigation via Blazor `NavigationManager`

## Current Focus

MVP Feature Parity — Complete remaining features needed to compete with free Cozi tier.

See **[docs/BACKLOG.md](docs/BACKLOG.md)** for the prioritized task backlog.

## Important Notes

- There is a `Denly.Models.DevicePlatform` that shadows `Microsoft.Maui.Devices.DevicePlatform`. Use the simple `FilePicker.Default.PickAsync()` overload to avoid conflicts.
- Settlement model has `ConfirmedAt`/`ConfirmedBy` columns (added Jan 2026). Migration: `20260128000000_settlement_confirmation.sql`.
- `NullableDateTimeOrArrayConverter` is used on nullable `DateTime?` columns that Supabase may return as arrays.
- Home page expense tiles use computed properties (not readonly mock data). Real data flows from `ExpenseService.GetBalancesAsync()` and `GetExpensesAsync()`.
