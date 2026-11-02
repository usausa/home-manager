INSERT INTO
    core.Setting (Key, Value, UpdatedAt)
VALUES
    (/*@ key */'', CAST(/*@ value */'{}' AS JSONB), /*@ updatedAt */'')
ON CONFLICT (Key) DO NOTHING
