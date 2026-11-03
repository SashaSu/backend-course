-- 04. Матчи 2024 года, продлившиеся дольше часа.
-- duration_sec строго больше 3600; год начала — 2024 по UTC,
-- started_at выводится как лежит в таблице, без приведения к дате.
--
-- Колонки: match_id, started_at, duration_sec, winner
-- Порядок: started_at по возрастанию
-- Строк:   78

SELECT match_id, started_at, duration_sec, winner
FROM matches
WHERE started_at >= TIMESTAMPTZ '2024-01-01 00:00:00+00'
  AND started_at <  TIMESTAMPTZ '2025-01-01 00:00:00+00'
  AND duration_sec > 3600
ORDER BY started_at ASC;