CREATE TABLE IF NOT EXISTS notification_outbox (
    id BIGSERIAL PRIMARY KEY,
    message_id UUID NOT NULL UNIQUE,
    topic TEXT NOT NULL,
    message_key TEXT NULL,
    payload JSONB NOT NULL,
    status TEXT NOT NULL DEFAULT 'Pending',
    attempt_count INTEGER NOT NULL DEFAULT 0,
    last_error TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMPTZ NULL,
    CONSTRAINT ck_notification_outbox_status
        CHECK (status IN ('Pending', 'Processed'))
);

CREATE INDEX IF NOT EXISTS ix_notification_outbox_pending
    ON notification_outbox (created_at, id)
    WHERE status = 'Pending';
