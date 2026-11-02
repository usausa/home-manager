-- P1: 全体設定とユーザー(docs/09-db-design.md §5-1・§5-2)
-- SQLite: 型を守るためSTRICTにする。時刻はUTCのISO 8601の文字列、JSONは文字列(json_validで確かめる)
-- SchemaVersion は DatabaseService が起動時に作る

CREATE TABLE IF NOT EXISTS Setting (
    Key        TEXT     NOT NULL,
    Value      TEXT     NOT NULL CHECK (json_valid(Value)),
    UpdatedAt  TEXT     NOT NULL,
    Version    INTEGER  NOT NULL DEFAULT 1,
    PRIMARY KEY (Key)
) STRICT;

CREATE TABLE IF NOT EXISTS Account (
    Id                  INTEGER  NOT NULL,
    Name                TEXT     NOT NULL,
    DisplayName         TEXT     NOT NULL,
    Password            BLOB     NOT NULL,
    Role                TEXT     NOT NULL,
    IsEnabled           INTEGER  NOT NULL DEFAULT 1,
    MustChangePassword  INTEGER  NOT NULL DEFAULT 0,
    Preferences         TEXT     NOT NULL DEFAULT '{}' CHECK (json_valid(Preferences)),
    LastLoginAt         TEXT,
    CreatedAt           TEXT     NOT NULL,
    UpdatedAt           TEXT     NOT NULL,
    Version             INTEGER  NOT NULL DEFAULT 1,
    PRIMARY KEY (Id AUTOINCREMENT),
    UNIQUE (Name)
) STRICT;
