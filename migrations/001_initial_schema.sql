CREATE TABLE IF NOT EXISTS chats (
    id BIGINT PRIMARY KEY,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS links (
    id BIGSERIAL PRIMARY KEY,
    url TEXT NOT NULL UNIQUE,
    last_checked_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS chat_links (
    chat_id BIGINT NOT NULL REFERENCES chats(id) ON DELETE CASCADE,
    link_id BIGINT NOT NULL REFERENCES links(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (chat_id, link_id)
);

CREATE TABLE IF NOT EXISTS tags (
    id BIGSERIAL PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS chat_link_tags (
    chat_id BIGINT NOT NULL,
    link_id BIGINT NOT NULL,
    tag_id BIGINT NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
    PRIMARY KEY (chat_id, link_id, tag_id),
    FOREIGN KEY (chat_id, link_id)
        REFERENCES chat_links(chat_id, link_id)
        ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_links_url ON links(url);
CREATE INDEX IF NOT EXISTS ix_chat_links_link_id ON chat_links(link_id);
CREATE INDEX IF NOT EXISTS ix_chat_link_tags_tag_id ON chat_link_tags(tag_id);
CREATE INDEX IF NOT EXISTS ix_tags_name ON tags(name);