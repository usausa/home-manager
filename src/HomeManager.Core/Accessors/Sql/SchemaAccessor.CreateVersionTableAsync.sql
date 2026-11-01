CREATE SCHEMA IF NOT EXISTS core;

CREATE TABLE IF NOT EXISTS core.SchemaVersion (
    Version    INTEGER      NOT NULL,
    Name       TEXT         NOT NULL,
    Checksum   TEXT         NOT NULL,
    AppliedAt  TIMESTAMPTZ  NOT NULL,
    PRIMARY KEY (Version)
)
