# CLAUDE.md

## Project: Denly

A co-parenting coordination app built by GuildStack Labs.

## Mission

Fair-priced, community-driven alternative to expensive co-parenting apps like OurFamilyWizard ($100-200/year). Pay-what-you-want model ($0-15/year suggested).

## Tech Stack

- .NET MAUI Blazor Hybrid
- Target platforms: iOS and Android
- Backend: Supabase (PostgreSQL + Auth + Storage)

## MVP Features (3 only)

1. **Shared Calendar** - Custody schedule, appointments
2. **Expense Tracker** - Log, split, track payments
3. **Document Storage** - Medical records, school info

## Core Values

- **Security-first** - Protecting family data
- **Radical cost transparency**
- **Open source**
- **No profit extraction** - Sustainability only

## Timeline

6 months to App Store launch

## Roadmap & Priorities

### 1. Foundation & Polish (Current Priority)
*Goal: Ensure the MVP is stable, fluid, and bug-free before adding complexity.*

-   **UI/UX Polish**: Add loading states, improve error feedback, and smooth out navigation.
-   **Code Quality**: Refactor services for better error handling and separation of concerns.
-   **Security Check**: Audit all data access points for proper RLS and auth checks.

### 2. Feature Enhancements (Post-Stabilization)

#### Shared Costs - Timeline View
-   Restyle expense list as a visual timeline.
-   Show settlements as distinct "milestone" markers.
-   Add filtering: by date range, category, settled/unsettled status.

### 3. Future Architecture
-   **Offline Capabilities**: Caching strategy for offline access.
-   **CI/CD**: Automated builds for iOS/Android.
-   **Testing**: Unit and integration tests (xUnit/bUnit).
