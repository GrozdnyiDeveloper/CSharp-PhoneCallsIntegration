--Звонки MightyCall
SELECT
    CAST(StartTime AS DATE) as date, -- Период
    COUNT(*) as call_count, -- Количество звонков за период
	COUNT(SessionID) as session_count, -- Количество сессий за период
	COUNT(QueueID) as queue_count -- Количество очередей за период
FROM
    IV_CallRecord
WHERE
    CAST(StartTime AS DATE) >= DATEADD(DAY, -14, CAST(GETDATE() AS DATE))
    AND CAST(StartTime AS DATE) < CAST(GETDATE() AS DATE) -- исключаем сегодня
GROUP BY
    CAST(StartTime AS DATE)
ORDER BY
    date DESC;


--Все звонки
SELECT
    CAST(StartTime AS DATE) as date, -- Период
    COUNT(*) as call_count, -- Количество звонков за период
	COUNT(CASE WHEN UserID1 = 'External0' OR UserID2 = 'External0'  THEN 1 END) as external_count, -- Количество внешних звонков за период
	COUNT(CASE WHEN UserID1 != 'External0' AND UserID2 != 'External0' THEN 1 END) as internal_count -- Количество внутренних звонков за период
FROM
    SCL2_Conversation2
WHERE
    CAST(StartTime AS DATE) >= DATEADD(DAY, -14, CAST(GETDATE() AS DATE))
    AND CAST(StartTime AS DATE) < CAST(GETDATE() AS DATE) -- исключаем сегодня
GROUP BY
    CAST(StartTime AS DATE)
ORDER BY
    date DESC;


--Cовершившиеся звонки
SELECT
    CAST(StartTime AS DATE) as date, -- Период
    COUNT(*) as call_count, -- Количество звонков за период
	COUNT(CASE WHEN UserID1 = 'External0' OR UserID2 = 'External0'  THEN 1 END) as external_count, -- Количество внешних звонков за период
	COUNT(CASE WHEN UserID1 != 'External0' AND UserID2 != 'External0' THEN 1 END) as internal_count -- Количество внутренних звонков за период
FROM
    SCL2_Conversation
WHERE
    CAST(StartTime AS DATE) >= DATEADD(DAY, -14, CAST(GETDATE() AS DATE))
    AND CAST(StartTime AS DATE) < CAST(GETDATE() AS DATE) -- исключаем сегодня
GROUP BY
    CAST(StartTime AS DATE)
ORDER BY
    date DESC;