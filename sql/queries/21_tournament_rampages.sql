-- 21. Турнирные выступления с KDA выше шестикратного среднего.
-- KDA = (kills + assists)::numeric / GREATEST(deaths, 1); порог — строго больше шести
-- средних KDA по всей match_players; только матчи турниров; kda округлить до 2 знаков.
--
-- Колонки: match_id, tournament, player, hero, kda
-- Порядок: kda по убыванию, при равенстве match_id, затем slot
-- Строк:   60

WITH player_kda AS (SELECT mp.*,
                           (mp.kills + mp.assists) ::numeric / GREATEST(mp.deaths, 1) AS kda
                    FROM match_players AS mp),
     threshold AS (SELECT 6 * AVG(kda) AS limit_kda
                   FROM player_kda)
SELECT m.match_id,
       t.name           AS tournament,
       p.nickname       AS player,
       h.name           AS hero,
       ROUND(pk.kda, 2) AS kda
FROM player_kda AS pk
         JOIN matches AS m ON m.match_id = pk.match_id
         JOIN tournaments AS t ON t.tournament_id = m.tournament_id
         JOIN players AS p ON p.player_id = pk.player_id
         JOIN heroes AS h ON h.hero_id = pk.hero_id
         CROSS JOIN threshold AS th
WHERE m.tournament_id IS NOT NULL
  AND pk.kda > th.limit_kda
ORDER BY kda DESC, m.match_id ASC, pk.slot ASC;
