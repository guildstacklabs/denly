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

```
User
├── id (uuid)
├── email
├── name
├── avatar_url (optional)
└── created_at

Den
├── id (uuid)
├── name
├── created_at
└── created_by → User.id

DenMember
├── id (uuid)
├── den_id → Den.id
├── user_id → User.id
├── role: enum (owner, co-parent, observer)
├── invited_by → User.id
├── joined_at
└── UNIQUE(den_id, user_id)

Child
├── id (uuid)
├── den_id → Den.id
├── name
├── birth_date (optional)
├── color (for UI: hex or preset name)
└── created_at

Event
├── id (uuid)
├── den_id → Den.id
├── child_id → Child.id (optional, null = all children)
├── title
├── event_type: enum (handoff, doctor, school, activity, family, other)
├── starts_at
├── ends_at (optional, null = no end time)
├── all_day: boolean
├── location (optional)
├── notes (optional)
├── created_by → User.id
└── created_at

Expense
├── id (uuid)
├── den_id → Den.id
├── child_id → Child.id (optional, null = shared/household)
├── description
├── amount (decimal)
├── paid_by → User.id
├── receipt_url (optional)
├── created_by → User.id
├── created_at
└── settled_at (null until part of a settlement)

Settlement
├── id (uuid)
├── den_id → Den.id
├── from_user_id → User.id (who paid)
├── to_user_id → User.id (who received)
├── amount (decimal)
├── note (optional)
├── created_by → User.id
└── created_at

Document
├── id (uuid)
├── den_id → Den.id
├── child_id → Child.id (optional, null = den-wide like custody agreement)
├── title
├── category: enum (medical, school, legal, identity, other)
├── file_url
├── uploaded_by → User.id
└── created_at
```

### Notes on the Model

**child_id is optional on Event, Expense, Document:** This allows "family dinner" events, "groceries" expenses, or "custody agreement" documents that apply to the whole den, not a specific child.

**Settlements are separate from Expenses:** An expense creates a balance. A settlement zeroes it out. This matches your existing flow and keeps the accounting clean.

**No split_type or percentage fields:** For MVP, assume 50/50 split. Can add complexity later if users request it. Most co-parents split equally or have fixed arrangements they track mentally.

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

## Migration Path

**MVP (Current - Local Storage):**
- Single implicit den
- No auth, no members
- Data model matches schema above minus user relationships

**V1 (Supabase):**
- Full schema with auth
- Single den per user (simplify onboarding)
- Invite co-parent flow

**V1.5:**
- Multi-den support
- Den switcher UI
- Observer invites

**V2 (If needed):**
- Ownership transfer
- Member removal flow
- Data export on departure

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
