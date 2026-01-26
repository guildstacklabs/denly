-- Phase 4: UI data model updates (Shared Costs, Schedule Updated State)

-- 1) Den time zone (IANA)
ALTER TABLE dens ADD COLUMN IF NOT EXISTS time_zone text DEFAULT 'UTC';

-- 2) Event updated_at timestamp
ALTER TABLE events ADD COLUMN IF NOT EXISTS updated_at timestamptz DEFAULT now();

UPDATE events SET updated_at = COALESCE(updated_at, created_at, now());

CREATE OR REPLACE FUNCTION update_events_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS events_updated_at ON events;
CREATE TRIGGER events_updated_at
    BEFORE UPDATE ON events
    FOR EACH ROW
    EXECUTE FUNCTION update_events_updated_at();

-- 3) Event seen state
CREATE TABLE IF NOT EXISTS event_seen (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    den_id uuid NOT NULL REFERENCES dens(id) ON DELETE CASCADE,
    event_id uuid NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
    last_seen_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (event_id, user_id)
);

CREATE INDEX IF NOT EXISTS idx_event_seen_den_id ON event_seen(den_id);
CREATE INDEX IF NOT EXISTS idx_event_seen_event_id ON event_seen(event_id);
CREATE INDEX IF NOT EXISTS idx_event_seen_user_id ON event_seen(user_id);

ALTER TABLE event_seen ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Event seen: members can read"
    ON event_seen FOR SELECT
    USING (EXISTS (
        SELECT 1 FROM den_members m
        WHERE m.den_id = event_seen.den_id
          AND m.user_id = auth.uid()
    ));

CREATE POLICY "Event seen: members can insert own"
    ON event_seen FOR INSERT
    WITH CHECK (
        user_id = auth.uid() AND EXISTS (
            SELECT 1 FROM den_members m
            WHERE m.den_id = event_seen.den_id
              AND m.user_id = auth.uid()
        )
    );

CREATE POLICY "Event seen: members can update own"
    ON event_seen FOR UPDATE
    USING (
        user_id = auth.uid() AND EXISTS (
            SELECT 1 FROM den_members m
            WHERE m.den_id = event_seen.den_id
              AND m.user_id = auth.uid()
        )
    )
    WITH CHECK (user_id = auth.uid());

CREATE POLICY "Event seen: members can delete own"
    ON event_seen FOR DELETE
    USING (user_id = auth.uid());

-- 4) Event attachments
CREATE TABLE IF NOT EXISTS event_attachments (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    den_id uuid NOT NULL REFERENCES dens(id) ON DELETE CASCADE,
    event_id uuid NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    title text,
    file_url text NOT NULL,
    created_by uuid NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_event_attachments_den_id ON event_attachments(den_id);
CREATE INDEX IF NOT EXISTS idx_event_attachments_event_id ON event_attachments(event_id);

ALTER TABLE event_attachments ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Event attachments: members can read"
    ON event_attachments FOR SELECT
    USING (EXISTS (
        SELECT 1 FROM den_members m
        WHERE m.den_id = event_attachments.den_id
          AND m.user_id = auth.uid()
    ));

CREATE POLICY "Event attachments: members can insert"
    ON event_attachments FOR INSERT
    WITH CHECK (
        created_by = auth.uid() AND EXISTS (
            SELECT 1 FROM den_members m
            WHERE m.den_id = event_attachments.den_id
              AND m.user_id = auth.uid()
        )
    );

CREATE POLICY "Event attachments: members can update own"
    ON event_attachments FOR UPDATE
    USING (created_by = auth.uid())
    WITH CHECK (created_by = auth.uid());

CREATE POLICY "Event attachments: members can delete own"
    ON event_attachments FOR DELETE
    USING (created_by = auth.uid());

-- 5) Expense splits (per-user split)
CREATE TABLE IF NOT EXISTS expense_splits (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    den_id uuid NOT NULL REFERENCES dens(id) ON DELETE CASCADE,
    expense_id uuid NOT NULL REFERENCES expenses(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
    percent numeric(5,2) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (expense_id, user_id)
);

CREATE INDEX IF NOT EXISTS idx_expense_splits_den_id ON expense_splits(den_id);
CREATE INDEX IF NOT EXISTS idx_expense_splits_expense_id ON expense_splits(expense_id);

ALTER TABLE expense_splits ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Expense splits: members can read"
    ON expense_splits FOR SELECT
    USING (EXISTS (
        SELECT 1 FROM den_members m
        WHERE m.den_id = expense_splits.den_id
          AND m.user_id = auth.uid()
    ));

CREATE POLICY "Expense splits: members can insert"
    ON expense_splits FOR INSERT
    WITH CHECK (EXISTS (
        SELECT 1 FROM den_members m
        WHERE m.den_id = expense_splits.den_id
          AND m.user_id = auth.uid()
    ));

CREATE POLICY "Expense splits: members can update"
    ON expense_splits FOR UPDATE
    USING (EXISTS (
        SELECT 1 FROM den_members m
        WHERE m.den_id = expense_splits.den_id
          AND m.user_id = auth.uid()
    ))
    WITH CHECK (EXISTS (
        SELECT 1 FROM den_members m
        WHERE m.den_id = expense_splits.den_id
          AND m.user_id = auth.uid()
    ));

CREATE POLICY "Expense splits: members can delete"
    ON expense_splits FOR DELETE
    USING (EXISTS (
        SELECT 1 FROM den_members m
        WHERE m.den_id = expense_splits.den_id
          AND m.user_id = auth.uid()
    ));
