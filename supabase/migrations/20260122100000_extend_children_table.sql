-- Extend children table for full name management and soft-delete
-- Migrates existing 'name' to 'first_name', adds middle_name, last_name, deactivated_at

-- Step 1: Rename 'name' to 'first_name'
ALTER TABLE children RENAME COLUMN name TO first_name;

-- Step 2: Add new columns
ALTER TABLE children ADD COLUMN middle_name text;
ALTER TABLE children ADD COLUMN last_name text;
ALTER TABLE children ADD COLUMN deactivated_at timestamptz;

-- Step 3: Add Info Bank columns if they don't exist (schema sync)
-- These may already exist from model additions
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'children' AND column_name = 'doctor_name') THEN
        ALTER TABLE children ADD COLUMN doctor_name text;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'children' AND column_name = 'doctor_contact') THEN
        ALTER TABLE children ADD COLUMN doctor_contact text;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'children' AND column_name = 'allergies') THEN
        ALTER TABLE children ADD COLUMN allergies text;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'children' AND column_name = 'school_name') THEN
        ALTER TABLE children ADD COLUMN school_name text;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'children' AND column_name = 'clothing_size') THEN
        ALTER TABLE children ADD COLUMN clothing_size text;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'children' AND column_name = 'shoe_size') THEN
        ALTER TABLE children ADD COLUMN shoe_size text;
    END IF;
END $$;

-- Step 4: Create index for filtering active children
CREATE INDEX idx_children_active ON children(den_id) WHERE deactivated_at IS NULL;

-- Step 5: Add comment for documentation
COMMENT ON COLUMN children.deactivated_at IS 'Soft-delete timestamp. NULL = active, set = deactivated.';
