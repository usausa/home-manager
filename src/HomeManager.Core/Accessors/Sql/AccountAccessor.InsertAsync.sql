INSERT INTO
    Account (Name, DisplayName, Password, Role, MustChangePassword, CreatedAt, UpdatedAt)
VALUES
    (/*@ name */'', /*@ displayName */'', /*@ password */NULL, /*@ role */'', /*@ mustChangePassword */FALSE, /*@ createdAt */'', /*@ createdAt */'')
RETURNING
    Id
