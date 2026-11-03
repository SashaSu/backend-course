-- 18. Призовые турниров, до которых добрались игроки команды.
-- Турнир в зачёт один раз, если игрок сыграл его матч в составе: joined_at <=
-- started_at::date < left_at (пустой left_at — сейчас); команды без турниров не выводятся.
--
-- Колонки: team, tag, tournaments, prize_total
-- Порядок: prize_total по убыванию, при равенстве team по возрастанию
-- Строк:   22

WITH team_tournaments AS (SELECT DISTINCT tm.team_id, m.tournament_id
                          FROM team_members AS tm
                                   JOIN match_players AS mp ON mp.player_id = tm.player_id
                                   JOIN matches AS m ON m.match_id = mp.match_id
                                   JOIN tournaments AS tr ON tr.tournament_id = m.tournament_id
                          WHERE (m.started_at AT TIME ZONE 'UTC')
    :: date >= tm.joined_at
    AND (
    tm.left_at IS NULL
    OR (m.started_at AT TIME ZONE 'UTC'):: date
   < tm.left_at
    )
    )
SELECT t.name             AS team,
       t.tag,
       COUNT(*)           AS tournaments,
       SUM(tr.prize_pool) AS prize_total
FROM team_tournaments AS tt
         JOIN teams AS t ON t.team_id = tt.team_id
         JOIN tournaments AS tr ON tr.tournament_id = tt.tournament_id
GROUP BY t.team_id, t.name, t.tag
ORDER BY prize_total DESC, team ASC;