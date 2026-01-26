# Denly UI Design Specification
## Mobile-First · Modern Sanctuary · Quiet Glass

This document defines the **visual design, layout rules, and aesthetic constraints** for Denly’s mobile application.  
It is intended to guide Codex in generating UI changes that are **consistent, calm, and emotionally neutral**, especially for co-parenting contexts.

---

## 1. Design Philosophy — “Modern Sanctuary”

Denly is not a dashboard.  
It is a **digital sanctuary**: calm, predictable, and non-reactive.

Core principles:
- Soft over sharp
- Read-only first
- No urgency language
- No visual competition
- One-thumb mobile usage

The chosen aesthetic style is **Quiet Glass**:
- Subtle translucency
- Soft elevation
- Minimal contrast
- Clear hierarchy without harsh borders

---

## 2. Global Layout Rules

### Mobile First
- All layouts are designed for **phone screens**
- Vertical scrolling only
- No side-by-side content
- One primary column

### Spacing & Shape
- Rounded corners throughout (no sharp rectangles)
- Cards separated by **space**, not lines
- Generous vertical padding
- Minimal horizontal padding to maximize content width

### Visual Weight
- Schedule > everything else
- Secondary information must never visually compete
- No bright accent colors for primary content

---

## 3. Persistent Navigation

### Bottom Navigation Bar
Visible on all main screens.

Tabs (left → right):
1. Home
2. Calendar
3. Expenses
4. Info

Rules:
- Icons + labels
- Subtle active state only
- No animation or bounce
- Navigation bar feels “resting”, not highlighted

---

## 4. Home Screen (Today)

### Purpose
Provide a **calm snapshot of life**, not a control panel.

### Structure (top → bottom)

#### 4.1 Sanctuary Header
- Page title: **“Today”**
- Secondary text: “Last updated a short while ago”
- Soft collapsing behavior:
  - Fully visible at top
  - Gradually fades/shrinks on scroll
  - Reappears when scrolling up
- No buttons
- No actions
- No notifications

#### 4.2 4-Day Schedule Spine (Primary Content)
- Exactly **four day cards**
- Current day always first
- Each day is its own rounded card
- All days have **identical structure**

Each day card includes:
- Day name (primary)
- Date (secondary)
- Up to 2–3 event rows
- Overflow text: “+N more items” (neutral wording)

Event rows:
- Time (if applicable)
- Neutral event title
- Child names as **plain text only**
  - No colors
  - No avatars
  - No icons

No nesting.  
No mixed layouts.  
Each day card is visually independent.

#### 4.3 Life Snapshot Cards (Secondary)
Appears **below** the schedule.

Cards:
- Shared Costs (summary only)
- Child Info (chips only)

Rules:
- Smaller than day cards
- Visually secondary
- No lists longer than one item
- No alerts or warnings

---

## 5. Calendar Screen

### Purpose
Provide **temporal clarity**, not financial or administrative info.

### Explicit Rule
**Shared Costs MUST NOT appear on the Calendar screen.**

### Structure

#### 5.1 Calendar Header
- Title: “Calendar”
- Month + year centered
- Left/right arrows for month navigation

#### 5.2 Month Grid
- Standard month grid
- Days with events indicated subtly (dot or soft highlight)
- Selected day uses gentle emphasis
- Today is identifiable but not dominant

#### 5.3 Day Agenda Preview
- Appears below the calendar grid
- Shows events for the selected day
- Same event row style as Home
- Uses identical neutral language

No cost data.  
No balances.  
No financial summaries.

---

## 6. Expenses Screen

### Purpose
Contain **all shared financial information** in one place.

### Structure

#### 6.1 Expenses Header
- Title: “Expenses”
- Simple back affordance if navigated deep
- No collapsing behavior

#### 6.2 Balance Summary Card
- Shows current shared balance
- Calm wording (no urgency)
- Neutral tone:
  - “Shared balance”
  - “Nothing to settle right now”

#### 6.3 Recent Expenses List
- Rounded list items
- Each item shows:
  - Description
  - Who added/paid
  - Amount
- No color coding for people
- No warnings or alerts by default

#### 6.4 View All Expenses
- Single call-to-action at bottom
- Low emphasis

---

## 7. Language & Microcopy Rules

- No emotionally charged words
- No urgency language (“due”, “late”, “overdue”)
- Prefer:
  - “items” over “events”
  - “updated” over “synced”
  - “nothing to settle” over “no balance due”

Avoid:
- Possessive framing
- Imperatives
- Positive or negative judgment

The UI should read like a **shared logbook**.

---

## 8. Visual Consistency Rules (Critical)

- Identical components must look identical everywhere
- Day cards must never change structure
- Events must never nest visually
- Financial information must NEVER appear outside Expenses
- Calendar is time-only
- Home is snapshot-only
- Expenses are finance-only

---

## 9. What Codex Should Optimize For

When generating or modifying UI:
- Reduce cognitive load
- Reduce visual noise
- Preserve emotional neutrality
- Preserve layout consistency
- Preserve predictability across screens

If a decision increases clarity but adds urgency — **do not do it**.

---

## 10. Summary (One Sentence)

Denly’s UI should feel like **opening a calm family notebook**, not operating an app.
