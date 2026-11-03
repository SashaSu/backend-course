-- 13. Сколько матчей сыграно в каждом году.
-- Год из started_at; tournament_matches — матчи с непустым tournament_id, ноль допустим.
--
-- Колонки: year, matches, tournament_matches
-- Порядок: year по возрастанию
-- Строк:   10

SELECT EXTRACT(YEAR FROM started_at AT TIME ZONE 'UTC') ::integer AS year,
       COUNT(*) AS matches,
       COUNT(*) FILTER (WHERE tournament_id IS NOT NULL) AS tournament_matches
FROM matches
GROUP BY EXTRACT (YEAR FROM started_at AT TIME ZONE 'UTC'):: integer
ORDER BY year ASC;