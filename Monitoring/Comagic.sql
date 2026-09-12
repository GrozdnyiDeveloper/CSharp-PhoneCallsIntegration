--Звонки Comagic
SELECT
    CAST(call_date AS DATE) as date, -- Дата
    COUNT(*) as call_count, -- Количество звонков за период
	COUNT(CASE WHEN is_processed = 1 THEN 1 END) as processed_count, -- Количество обработанных звонков (установление связи с MightyCall)
	COUNT(CASE WHEN call_external_id is not null THEN 1 END) as mightycall_count -- Количество звонков связанных с MightyCall
FROM
    comagic_calls
WHERE
    call_date >= DATEADD(DAY, -14, CAST(GETDATE() AS DATE))
    AND call_date < CAST(GETDATE() AS DATE) -- исключаем сегодня
GROUP BY
    CAST(call_date AS DATE)
ORDER BY
    date DESC;


--Тэги звонков Comagic
SELECT 
	CAST(tag_change_time AS DATE) as date, -- Дата
    COUNT(*) as tag_count, -- Количество изменённых тэгов
	COUNT(CASE WHEN tag_type = 'manual' THEN 1 END) as manual_count, -- Количество изменённых вручную тэгов
    COUNT(CASE WHEN tag_type = 'auto' THEN 1 END) as auto_count -- Количество автоматически тэгов
FROM
    comagic_calls_tags
WHERE
    tag_change_time >= DATEADD(DAY, -14, CAST(GETDATE() AS DATE))
    AND tag_change_time < CAST(GETDATE() AS DATE) -- исключаем сегодня
GROUP BY
    CAST(tag_change_time AS DATE)
ORDER BY
    date DESC;


-- Заявки Comagic
SELECT
    CAST(date_time AS DATE) as date, -- Дата
    COUNT(*) as message_count, -- Количество заявок
	COUNT(CASE WHEN communication_id is not null THEN 1 END) as communication_count -- Количество обращений
FROM
    comagic_messages
WHERE
    date_time >= DATEADD(DAY, -14, CAST(GETDATE() AS DATE))
    AND date_time < CAST(GETDATE() AS DATE) -- исключаем сегодня
GROUP BY
    CAST(date_time AS DATE)
ORDER BY
    date DESC;


--Тэги заявок Comagic
SELECT 
    COUNT(*) as all_count, -- Общее количество тегов
    COUNT(CASE WHEN tag_type = 'manual' THEN 1 END) as manual_count, -- Обработанные вручную теги 
    COUNT(CASE WHEN tag_type = 'auto' THEN 1 END) as auto_count -- Обработанные автоматически теги 
FROM
    comagic_messages_tags


--Тэги заявок Comagic (разбивка по названию)
SELECT 
    tag_name, -- Название тега
    COUNT(*) as all_count, -- Общее количество тегов
    COUNT(CASE WHEN tag_type = 'manual' THEN 1 END) as manual_count, -- Обработанные вручную теги 
    COUNT(CASE WHEN tag_type = 'auto' THEN 1 END) as auto_count -- Обработанные автоматически теги 
FROM
    comagic_messages_tags
GROUP BY
    tag_name

--Внутренние звонки Cassandra
SELECT
    CAST(day AS DATE) as date, -- Дата
	COUNT(*) as call_count, -- Количество звонков за период
    COUNT(CASE WHEN call_status = 'MISSED' THEN 1 END) as missed_count, -- Количество пропущенных звонков за период
	COUNT(CASE WHEN call_status = 'ANSWERED' THEN 1 END) as answered_count -- Количество обработанных звонков за период
FROM
    InboundCallsTable
WHERE
    day >= DATEADD(DAY, -14, CAST(GETDATE() AS DATE))
    AND day < CAST(GETDATE() AS DATE) -- исключаем сегодня
GROUP BY
    CAST(day AS DATE)
ORDER BY
    date DESC;

--Внешние звонки Cassandra
SELECT
    CAST(day AS DATE) as date, -- Дата
	COUNT(*) as call_count, -- Количество звонков за период
    COUNT(CASE WHEN call_status = 'MISSED' THEN 1 END) as missed_count, -- Количество пропущенных звонков за период
	COUNT(CASE WHEN call_status = 'ANSWERED' THEN 1 END) as answered_count -- Количество обработанных звонков за период
FROM
    OutboundCallsTable
WHERE
    day >= DATEADD(DAY, -14, CAST(GETDATE() AS DATE))
    AND day < CAST(GETDATE() AS DATE) -- исключаем сегодня
GROUP BY
    CAST(day AS DATE)
ORDER BY
    date DESC;