-- Add settlement confirmation columns

ALTER TABLE settlements ADD COLUMN IF NOT EXISTS confirmed_at timestamptz;
ALTER TABLE settlements ADD COLUMN IF NOT EXISTS confirmed_by uuid REFERENCES profiles(id);

-- Index for querying unconfirmed settlements
CREATE INDEX IF NOT EXISTS idx_settlements_unconfirmed ON settlements(den_id) WHERE confirmed_at IS NULL;
