-- P1: 全体設定とユーザー(docs/09-db-design.md §5-1・§5-2)
-- core.SchemaVersion は DatabaseService が起動時に作る

CREATE TABLE IF NOT EXISTS core.Setting (
    Key        TEXT         NOT NULL,
    Value      JSONB        NOT NULL,
    UpdatedAt  TIMESTAMPTZ  NOT NULL,
    Version    INTEGER      NOT NULL DEFAULT 1,
    PRIMARY KEY (Key)
);

CREATE TABLE IF NOT EXISTS core.Account (
    Id                  BIGINT       GENERATED ALWAYS AS IDENTITY,
    Name                TEXT         NOT NULL,
    DisplayName         TEXT         NOT NULL,
    Password            BYTEA        NOT NULL,
    Role                TEXT         NOT NULL,
    IsEnabled           BOOLEAN      NOT NULL DEFAULT TRUE,
    MustChangePassword  BOOLEAN      NOT NULL DEFAULT FALSE,
    Preferences         JSONB        NOT NULL DEFAULT '{}',
    LastLoginAt         TIMESTAMPTZ,
    CreatedAt           TIMESTAMPTZ  NOT NULL,
    UpdatedAt           TIMESTAMPTZ  NOT NULL,
    Version             INTEGER      NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    UNIQUE (Name)
);
