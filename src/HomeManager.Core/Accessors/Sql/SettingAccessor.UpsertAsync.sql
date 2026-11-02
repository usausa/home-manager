INSERT INTO
    core.Setting (Key, Value, UpdatedAt)
VALUES
    (/*@ key */'', CAST(/*@ value */'{}' AS JSONB), /*@ updatedAt */'')
ON CONFLICT (Key) DO UPDATE SET
    Value = EXCLUDED.Value,
    UpdatedAt = EXCLUDED.UpdatedAt,
    Version = core.Setting.Version + 1
