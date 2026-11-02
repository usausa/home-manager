UPDATE
    Setting
SET
    Value = /*@ value */'{}',
    UpdatedAt = /*@ updatedAt */'',
    Version = Version + 1
WHERE
    Key = /*@ key */''
    AND Version = /*@ version */1
