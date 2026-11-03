-- 25. Место стримера-профессионала в рейтинге своей страны.
-- Только стримеры с player_id и действующей записью в team_members; место по mmr
-- по убыванию и country_players считаются по всей players; player — ник из players.
--
-- Колонки: player, country, mmr, country_rank, country_players, team_tag, platform
-- Порядок: country по возрастанию, внутри страны country_rank по возрастанию
-- Строк:   16

WITH country_ranks AS (SELECT player_id,
                              country,
                              mmr,
                              RANK() OVER (PARTITION BY country ORDER BY mmr DESC) AS country_rank, COUNT(*) OVER (PARTITION BY country) AS country_players
                       FROM players)
SELECT p.nickname AS player,
       cr.country,
       cr.mmr,
       cr.country_rank,
       cr.country_players,
       t.tag      AS team_tag,
       pl.name    AS platform
FROM streamers AS s
         JOIN country_ranks AS cr ON cr.player_id = s.player_id
         JOIN players AS p ON p.player_id = s.player_id
         JOIN team_members AS tm
              ON tm.player_id = s.player_id
                  AND tm.left_at IS NULL
         JOIN teams AS t ON t.team_id = tm.team_id
         JOIN platforms AS pl ON pl.platform_id = s.platform_id
ORDER BY cr.country ASC, cr.country_rank ASC;
