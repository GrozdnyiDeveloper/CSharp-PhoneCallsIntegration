--Звонки Мегафон по дням за 2 недели
SELECT
    CAST(start AS DATE) as date, -- Дата
    COUNT(*) as call_count, -- Количество звонков за период
	COUNT(CASE WHEN call_external_id is not null THEN 1 END) as mightycall_count, -- Количество звонков связанных с MightyCall
	COUNT(CASE WHEN call_guid_crm is not null THEN 1 END) as crm_count, -- Количество звонков связанных с CRM
	COUNT(CASE WHEN is_external = 1 THEN 1 END) as external_count, -- Количество внешних звонков 
	COUNT(CASE WHEN is_external = 0 THEN 1 END) as internal_count -- Количество внутренних звонков 
FROM
    Megafon_calls
WHERE
    CAST(start AS DATE) >= DATEADD(DAY, -14, CAST(GETDATE() AS DATE))
    AND CAST(start AS DATE) < CAST(GETDATE() AS DATE) -- исключаем сегодня
GROUP BY
    CAST(start AS DATE)
ORDER BY
    date DESC;