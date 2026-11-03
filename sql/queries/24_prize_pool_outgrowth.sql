-- 24. Турниры, чей призовой фонд больше суммы призовых всех предыдущих.
-- Предыдущие — со строго меньшим started_at, сравнение строгое; у первого турнира
-- prev_total и prev_count нули, не NULL; matches_played — число матчей турнира.
--
-- Колонки: tournament, started_at, prize_pool, prev_total, prev_count, matches_played
-- Порядок: started_at по возрастанию
-- Строк:   2

WITH previous_totals AS (SELECT t.tournament_id,
                                t.name,
                                t.started_at,
                                t.prize_pool,
                                COUNT(prev.tournament_id)         AS prev_count,
                                COALESCE(SUM(prev.prize_pool), 0) AS prev_total
                         FROM tournaments AS t
                                  LEFT JOIN tournaments AS prev
                                            ON prev.started_at < t.started_at
                         GROUP BY t.tournament_id, t.name, t.started_at, t.prize_pool),
     match_counts AS (SELECT tournament_id, COUNT(*) AS matches_played
                      FROM matches
                      WHERE tournament_id IS NOT NULL
                      GROUP BY tournament_id)
SELECT pt.name                        AS tournament,
       pt.started_at,
       pt.prize_pool,
       pt.prev_total,
       pt.prev_count,
       COALESCE(mc.matches_played, 0) AS matches_played
FROM previous_totals AS pt
         LEFT JOIN match_counts AS mc ON mc.tournament_id = pt.tournament_id
WHERE pt.prize_pool > pt.prev_total
ORDER BY pt.started_at ASC;