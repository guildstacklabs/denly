-- Device tokens table for push notifications
-- Stores FCM (Android) and APNs (iOS) device tokens

CREATE TABLE IF NOT EXISTS device_tokens (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    platform text NOT NULL CHECK (platform IN ('ios', 'android')),
    token text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    -- Each user can have one token per platform (handles token refresh)
    UNIQUE (user_id, platform)
);

-- Index for looking up tokens by user
CREATE INDEX idx_device_tokens_user_id ON device_tokens(user_id);

-- Enable RLS
ALTER TABLE device_tokens ENABLE ROW LEVEL SECURITY;

-- Users can only read/write their own tokens
CREATE POLICY "Users can view own tokens"
    ON device_tokens FOR SELECT
    USING (auth.uid() = user_id);

CREATE POLICY "Users can insert own tokens"
    ON device_tokens FOR INSERT
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update own tokens"
    ON device_tokens FOR UPDATE
    USING (auth.uid() = user_id)
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can delete own tokens"
    ON device_tokens FOR DELETE
    USING (auth.uid() = user_id);

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_device_tokens_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER device_tokens_updated_at
    BEFORE UPDATE ON device_tokens
    FOR EACH ROW
    EXECUTE FUNCTION update_device_tokens_updated_at();

-- Grant service role access for Edge Functions to query all tokens
-- (needed for sending notifications to den members)
GRANT SELECT ON device_tokens TO service_role;
