CREATE TABLE IF NOT EXISTS SchemaVersion (
    Version    INTEGER  NOT NULL,
    Name       TEXT     NOT NULL,
    Checksum   TEXT     NOT NULL,
    AppliedAt  TEXT     NOT NULL,
    PRIMARY KEY (Version)
) STRICT
