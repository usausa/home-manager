UPDATE
    core.Account
SET
    Password = /*@ password */NULL,
    MustChangePassword = FALSE,
    UpdatedAt = /*@ updatedAt */'',
    Version = Version + 1
WHERE
    Id = /*@ id */1
