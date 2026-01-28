# Denly Database Schema

**Database:** Supabase (PostgreSQL)  
**Last Updated:** January 2026

## Tables

### profiles
Extends Supabase `auth.users`. Auto-created on signup via trigger.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| id | uuid | NO | PK, references auth.users |
| email | text | YES | User's email |
| name | text | YES | Display name |
| avatar_url | text | YES | Profile photo URL |
| created_at | timestamptz | YES | Default: now() |

### dens
A coordination space between co-parents for a specific set of children.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| id | uuid | NO | PK, auto-generated |
| name | text | NO | Den name |
| created_by | uuid | YES | FK → profiles.id |
| created_at | timestamptz | YES | Default: now() |

### den_members
Junction table linking users to dens with roles.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| id | uuid | NO | PK, auto-generated |
| den_id | uuid | NO | FK → dens.id (cascade delete) |
| user_id | uuid | NO | FK → profiles.id (cascade delete) |
| role | text | NO | 'owner', 'co-parent', or 'observer' |
| invited_by | uuid | YES | FK → profiles.id |
| joined_at | timestamptz | YES | Default: now() |

**Constraints:** UNIQUE(den_id, user_id)

### children
Children belonging to a den. Supports soft-delete via `deactivated_at`.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| id | uuid | NO | PK, auto-generated |
| den_id | uuid | NO | FK → dens.id (cascade delete) |
| first_name | text | NO | Child's first name |
| middle_name | text | YES | Child's middle name |
| last_name | text | YES | Child's last name |
| birth_date | date | YES | Optional birthdate |
| color | text | YES | UI color (hex or preset name) |
| doctor_name | text | YES | Primary doctor's name |
| doctor_contact | text | YES | Doctor's phone/email |
| allergies | text | YES | Known allergies |
| school_name | text | YES | School name |
| clothing_size | text | YES | Current clothing size |
| shoe_size | text | YES | Current shoe size |
| created_at | timestamptz | YES | Default: now() |
| deactivated_at | timestamptz | YES | Soft-delete timestamp (NULL = active) |

**Indexes:** `idx_children_active` on (den_id) WHERE deactivated_at IS NULL

### events
Calendar events (handoffs, appointments, activities, etc.)

> **Note:** The `child_id` column is deprecated. Use `event_children` junction table for multi-child associations.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| id | uuid | NO | PK, auto-generated |
| den_id | uuid | NO | FK → dens.id (cascade delete) |
| child_id | uuid | YES | FK → children.id (deprecated, use event_children) |
| title | text | NO | Event title |
| event_type | text | NO | 'handoff', 'doctor', 'school', 'activity', 'family', 'other' |
| starts_at | timestamptz | NO | Start date/time |
| ends_at | timestamptz | YES | End date/time (null = no end) |
| all_day | boolean | YES | Default: false |
| location | text | YES | Event location |
| notes | text | YES | Additional notes |
| created_by | uuid | YES | FK → profiles.id |
| created_at | timestamptz | YES | Default: now() |
| updated_at | timestamptz | YES | Default: now() |

### event_children
Junction table linking events to multiple children (many-to-many).

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| id | uuid | NO | PK, auto-generated |
| event_id | uuid | NO | FK → events.id (cascade delete) |
| child_id | uuid | NO | FK → children.id (cascade delete) |
| den_id | uuid | NO | FK → dens.id (cascade delete) |
| created_at | timestamptz | NO | Default: now() |

**Constraints:** UNIQUE(event_id, child_id)
**Indexes:** `idx_event_children_event` on (event_id), `idx_event_children_child` on (child_id)

### expenses
Shared expenses between co-parents.

> **Note:** The `child_id` column is deprecated. Use `expense_children` junction table for multi-child associations.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| id | uuid | NO | PK, auto-generated |
| den_id | uuid | NO | FK → dens.id (cascade delete) |
| child_id | uuid | YES | FK → children.id (deprecated, use expense_children) |
| description | text | NO | Expense description |
| amount | decimal(10,2) | NO | Amount in dollars |
| paid_by | uuid | YES | FK → profiles.id (who paid) |
| receipt_url | text | YES | Receipt image URL |
| created_by | uuid | YES | FK → profiles.id |
| created_at | timestamptz | YES | Default: now() |
| settled_at | timestamptz | YES | When included in settlement |
| split_percent | decimal | YES | Legacy split percentage |

### expense_children
Junction table linking expenses to multiple children (many-to-many).

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| id | uuid | NO | PK, auto-generated |
| expense_id | uuid | NO | FK → expenses.id (cascade delete) |
| child_id | uuid | NO | FK → children.id (cascade delete) |
| den_id | uuid | NO | FK → dens.id (cascade delete) |
| created_at | timestamptz | NO | Default: now() |

**Constraints:** UNIQUE(expense_id, child_id)
**Indexes:** `idx_expense_children_expense` on (expense_id), `idx_expense_children_child` on (child_id)

### expense_splits
Per-expense split percentages between co-parents.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| id | uuid | NO | PK, auto-generated |
| den_id | uuid | NO | FK → dens.id (cascade delete) |
| expense_id | text | NO | FK → expenses.id |
| user_id | text | NO | FK → profiles.id |
| percent | decimal | NO | Split percentage (e.g., 50, 60, 70, 80) |
| created_at | timestamptz | YES | Default: now() |

### settlements
Records of settling up balances between co-parents.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| id | uuid | NO | PK, auto-generated |
| den_id | uuid | NO | FK → dens.id (cascade delete) |
| from_user_id | uuid | YES | FK → profiles.id (who paid) |
| to_user_id | uuid | YES | FK → profiles.id (who received) |
| amount | decimal(10,2) | NO | Settlement amount |
| note | text | YES | Optional note |
| created_by | uuid | YES | FK → profiles.id |
| created_at | timestamptz | YES | Default: now() |
| confirmed_at | timestamptz | YES | When recipient confirmed receipt |
| confirmed_by | text | YES | User ID who confirmed |

### calendar_subscriptions
Tokens for ICS calendar feed access (used by external calendar apps).

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| id | uuid | NO | PK, auto-generated |
| den_id | uuid | NO | FK → dens.id (cascade delete) |
| user_id | uuid | NO | FK → auth.users (cascade delete) |
| token | text | NO | Unique 32-char hex token |
| created_at | timestamptz | NO | Default: now() |

**Constraints:** UNIQUE(den_id, user_id), UNIQUE(token)
**Indexes:** `idx_calendar_subscriptions_token` on (token)

### documents
Stored documents (medical records, school forms, IDs, etc.)

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| id | uuid | NO | PK, auto-generated |
| den_id | uuid | NO | FK → dens.id (cascade delete) |
| child_id | uuid | YES | FK → children.id (null = den-wide) |
| title | text | NO | Document title |
| category | text | NO | 'medical', 'school', 'legal', 'identity', 'other' |
| file_url | text | NO | File storage URL |
| uploaded_by | uuid | YES | FK → profiles.id |
| created_at | timestamptz | YES | Default: now() |

### den_invites
Invite codes for joining a den.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| id | uuid | NO | PK, auto-generated |
| den_id | uuid | YES | FK → dens.id (cascade delete) |
| code | text | NO | 8-char alphanumeric code (e.g., 'K7X9M2PQ') |
| role | text | YES | 'co-parent' or 'observer', default: 'co-parent' |
| created_by | uuid | YES | FK → profiles.id |
| created_at | timestamptz | YES | Default: now() |
| expires_at | timestamptz | YES | Default: now() + 3 days |
| used_by | uuid | YES | FK → profiles.id |
| used_at | timestamptz | YES | When code was used |

**Constraints:** UNIQUE(code)

### invite_attempts
Rate limiting for invite code attempts.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| id | uuid | NO | PK, auto-generated |
| user_id | uuid | YES | FK → profiles.id |
| attempted_at | timestamptz | YES | Default: now() |
| success | boolean | YES | Default: false |

---

## Row Level Security (RLS)

All tables have RLS enabled. Key policies:

- **Users can only access data in dens they belong to**
- **Owners and co-parents** can insert/update/delete
- **Observers** have read-only access
- **Invite codes** are readable by anyone (to validate) but only manageable by den owners

---

## Storage Buckets

| Bucket | Purpose |
|--------|---------|
| receipts | Expense receipt images |
| documents | Family vault documents |

---

## Auth

- **Providers:** Google, Email/Password
- **Profile trigger:** Auto-creates `profiles` row on signup
- **Session:** Stored locally, persists across app restarts

---

## Future: Immutable Messages

For legal defensibility, messages will need:
- SHA-256 hash of content stored at creation
- No UPDATE or DELETE allowed via RLS
- Audit trail with timestamps
- Litigation hold capability
