SELECT
    page_count * page_size
FROM
    pragma_page_count(),
    pragma_page_size()
