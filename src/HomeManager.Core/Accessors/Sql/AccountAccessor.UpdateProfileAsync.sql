UPDATE
    Account
SET
    DisplayName = /*@ displayName */'',
    Preferences = /*@ preferences */'{}',
    UpdatedAt = /*@ updatedAt */'',
    Version = Version + 1
WHERE
    Id = /*@ id */1
    AND Version = /*@ version */1
