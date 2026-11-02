UPDATE
    core.Account
SET
    DisplayName = /*@ displayName */'',
    Preferences = CAST(/*@ preferences */'{}' AS JSONB),
    UpdatedAt = /*@ updatedAt */'',
    Version = Version + 1
WHERE
    Id = /*@ id */1
    AND Version = /*@ version */1
