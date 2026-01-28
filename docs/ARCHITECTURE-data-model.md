# Denly Data Model Architecture

## Overview

This document defines how Denly models family structures. The design prioritizes simplicity while supporting real-world complexity: blended families, multiple co-parenting relationships, and third-party observers like attorneys or step-parents.

## Core Concept: The Den

A **Den** is a coordination space between co-parents for a specific set of children. It's not a representation of an entire family tree—it's a workspace for two people raising kids together.

**Key principle:** If you have children with multiple partners, you have multiple dens. Each den is completely separate.

### Example: Blended Family

Alex has kids with two different co-parents:

```
Den 1: "Alex & Jordan"
├── Members: Alex (owner), Jordan (co-parent)
├── Children: Emma, Liam
└── All schedules, expenses, documents for Emma & Liam

Den 2: "Alex & Casey"  
├── Members: Alex (owner), Casey (co-parent)
├── Children: Noah
└── All schedules, expenses, documents for Noah
```

- Alex sees both dens in their app, switches between them
- Jordan only sees Den 1, has no idea Den 2 exists
- Casey only sees Den 2, has no idea Den 1 exists
- Data is completely isolated

---

## Roles

Three roles, simple permissions:

| Role | Can View | Can Edit | Can Manage Members | Can Delete Den |
|------|----------|----------|-------------------|----------------|
| **Owner** | ✓ | ✓ | ✓ | ✓ |
| **Co-parent** | ✓ | ✓ | ✓ (invite only) | ✗ |
| **Observer** | ✓ | ✗ | ✗ | ✗ |

**Owner:** Created the den. Full control. Can transfer ownership to co-parent if needed.

**Co-parent:** Equal partner for day-to-day use. Can add/edit events, expenses, documents. Can invite new members (but not remove the owner).

**Observer:** Read-only access to everything in the den. Perfect for:
- Step-parents who want visibility
- Attorneys during custody proceedings
- Social workers
- Grandparents or other family members
- Mediators

---

## New User Flow

1. **Sign up** → User creates account
2. **Create den** → Names it (default: "My Family" or prompt for co-parent's name)
3. **Add children** → Name, optional birthdate, pick a color/avatar
4. **Invite co-parent** → Email invite → They join with co-parent role
5. **Optionally invite observers** → Email invite → They join with observer role

That's it. No complex setup wizards.

---

## Data Model

> **See [DATABASE.md](./DATABASE.md)** for the complete, authoritative database schema with all columns, types, and constraints.

### Entity Overview

```
profiles              - User accounts (extends Supabase auth.users)
dens                  - Coordination spaces between co-parents
den_members           - Junction: users ↔ dens with roles
children              - Children belonging to a den
events                - Calendar events (handoffs, appointments, etc.)
event_children        - Junction: events ↔ children (many-to-many)
expenses              - Shared expenses between co-parents
expense_children      - Junction: expenses ↔ children (many-to-many)
expense_splits        - Per-expense split percentages between co-parents
settlements           - Records of settling up balances
calendar_subscriptions - ICS feed tokens for external calendar apps
documents             - Stored documents (medical records, IDs, etc.)
den_invites           - Invite codes for joining a den
invite_attempts       - Rate limiting for invite code attempts
```

### Key Relationships

- **Den** → created_by → profiles
- **DenMember** → den_id → dens, user_id → profiles, invited_by → profiles
- **Child** → den_id → dens
- **Event** → den_id → dens; linked to children via **event_children** junction table
- **Expense** → den_id → dens, paid_by → profiles; linked to children via **expense_children** junction table
- **ExpenseSplit** → expense_id → expenses, user_id → profiles (per-expense split percentages)
- **Document** → den_id → dens, child_id → children (optional)
- **Settlement** → from_user_id/to_user_id → profiles; confirmed_at/confirmed_by for receipt confirmation
- **CalendarSubscription** → den_id → dens, user_id → auth.users; token-based ICS feed access

### Notes on the Model

**Multi-child associations via junction tables:** Events and expenses can be linked to multiple children through `event_children` and `expense_children` tables. The legacy `child_id` column on events/expenses is deprecated but retained for backward compatibility.

**Settlements are separate from Expenses:** An expense creates a balance. A settlement zeroes it out. When a settlement is created, all unsettled expenses are marked with `settled_at`. Recipients can confirm receipt via `confirmed_at`/`confirmed_by`.

**Flexible expense splits:** Expenses support configurable splits (50/50, 60/40, 70/30, 80/20) stored in the `expense_splits` table. Each split record tracks a user's percentage for a specific expense.

**ICS calendar subscriptions:** Users can subscribe to their den's calendar from external apps (Google Calendar, Apple Calendar) via a token-authenticated Edge Function that serves a live `.ics` feed.

---

## Row Level Security (Supabase)

Clean RLS policies since everything is den-scoped:

```sql
-- Users can only see dens they belong to
CREATE POLICY "den_member_access" ON den FOR SELECT
  USING (id IN (
    SELECT den_id FROM den_member WHERE user_id = auth.uid()
  ));

-- Users can only see children in their dens
CREATE POLICY "child_access" ON child FOR SELECT
  USING (den_id IN (
    SELECT den_id FROM den_member WHERE user_id = auth.uid()
  ));

-- Same pattern for event, expense, document, settlement

-- Only owners and co-parents can insert/update/delete
CREATE POLICY "child_write" ON child FOR INSERT
  USING (den_id IN (
    SELECT den_id FROM den_member 
    WHERE user_id = auth.uid() 
    AND role IN ('owner', 'co-parent')
  ));
```

Observers automatically get read access but fail on any write operation.

---

## Multi-Den UX

When a user belongs to multiple dens:

1. **Den switcher** in the UI (dropdown or tab bar)
2. **Last-used den** remembered as default
3. **Notifications** indicate which den they're from
4. **No cross-den data** ever—complete isolation

The app always operates in the context of one den at a time.

---

## Implementation Status

**V1 (Complete):**
- Full schema with Supabase auth (Google, Email/Password)
- Den creation and management
- Invite co-parent flow with 8-character codes
- Rate limiting on invite attempts
- All data stored in Supabase PostgreSQL
- File storage in Supabase Storage (receipts, documents)
- Multi-den support with den switcher UI
- Children management

**V2 (Future):**
- Observer invites
- Ownership transfer
- Member removal flow
- Data export on departure
- Notification preferences per den

> **See [BACKLOG.md](./BACKLOG.md)** for the detailed task backlog and prioritized improvements.

---

## Open Questions

1. **What happens when a co-parent leaves or is removed?**
   - Proposed: Soft delete membership, historical data preserved (their name on past expenses, etc.), they lose access

2. **Can an observer be upgraded to co-parent?**
   - Proposed: Yes, owner can change roles

3. **What if both parents want to be "owner"?**
   - Proposed: Doesn't matter functionally. Co-parent has same day-to-day powers. Owner is just "who can delete the den"—which should rarely/never happen

4. **Notification preferences per den?**
   - Proposed: Yes, eventually. User might want more notifications from contentious co-parent, fewer from amicable one

---

## Summary

- **Den** = workspace for two co-parents + their children
- **One user can be in multiple dens** (blended families)
- **Three roles:** Owner, Co-parent (full access), Observer (read-only)
- **All data scoped to den** — simple permissions, clean isolation
- **No per-child permissions** — if you're in the den, you see all kids in that den
