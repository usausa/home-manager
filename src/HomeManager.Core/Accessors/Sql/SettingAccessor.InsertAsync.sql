INSERT INTO
    Setting (Key, Value, UpdatedAt)
VALUES
    (/*@ key */'', /*@ value */'{}', /*@ updatedAt */'')
ON CONFLICT (Key) DO NOTHING
