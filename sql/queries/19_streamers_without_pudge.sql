-- 19. Стримеры, которые ни разу не брали Pudge.
-- Только стримеры с непустым player_id и без единой игры за 'Pudge'; team — тег
-- команды с left_at IS NULL, иначе пусто; games — все строки match_players игрока.
--
-- Колонки: streamer, platform, team, followers, games
-- Порядок: followers по убыванию, при равенстве streamer по возрастанию
-- Строк:   8

SELECT s.nickname         AS streamer,
       pl.name            AS platform,
       t.tag              AS team,
       s.followers,
       COUNT(mp.match_id) AS games
FROM streamers AS s
         JOIN platforms AS pl ON pl.platform_id = s.platform_id
         LEFT JOIN team_members AS tm
                   ON tm.player_id = s.player_id
                       AND tm.left_at IS NULL
         LEFT JOIN teams AS t ON t.team_id = tm.team_id
         LEFT JOIN match_players AS mp ON mp.player_id = s.player_id
WHERE s.player_id IS NOT NULL
  AND NOT EXISTS (SELECT 1
                  FROM match_players AS pudge_games
                           JOIN heroes AS h ON h.hero_id = pudge_games.hero_id
                  WHERE pudge_games.player_id = s.player_id
                    AND h.name = 'Pudge')
GROUP BY s.streamer_id, s.nickname, pl.name, t.tag, s.followers
ORDER BY s.followers DESC, streamer ASC;
