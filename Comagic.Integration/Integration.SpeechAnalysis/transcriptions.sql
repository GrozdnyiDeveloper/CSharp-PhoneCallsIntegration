WITH ranked_transcriptions AS (
    SELECT 
    task_id, call_id, transcription_raw, transcription_enhanced, duration, 
    language, config, created_at, summary, is_processed,
    ROW_NUMBER() OVER (PARTITION BY call_id ORDER BY created_at DESC) as rn
    FROM {transctiption_source}.transcriptions
)
SELECT task_id, call_id, transcription_raw, transcription_enhanced, duration, 
    language, config, created_at, summary, is_processed
FROM ranked_transcriptions
WHERE rn = 1 and is_processed <> true
ORDER BY call_id