UPDATE
    core.Setting
SET
    Value = CAST(/*@ value */'{}' AS JSONB),
    UpdatedAt = /*@ updatedAt */'',
    Version = Version + 1
WHERE
    Key = /*@ key */''
    AND Version = /*@ version */1
