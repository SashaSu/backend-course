-- 15. Сколько донатов собрал каждый стример.
-- В ответе все стримеры: без донатов donations = 0 и total_amount = 0.00;
-- team — команда с left_at IS NULL, иначе пусто.
--
-- Колонки: streamer, platform, team, donations, total_amount
-- Порядок: total_amount по убыванию, при равенстве streamer по возрастанию
-- Строк:   40

SELECT s.nickname           AS streamer,
       p.name               AS platform,
       t.name               AS team,
       COUNT(d.donation_id) AS donations,
       COALESCE(SUM(d.amount), 0.00) ::numeric(10,2) AS total_amount
FROM streamers AS s
         JOIN platforms AS p ON p.platform_id = s.platform_id
         LEFT JOIN streams AS st ON st.streamer_id = s.streamer_id
         LEFT JOIN stream_donations AS d ON d.stream_id = st.stream_id
         LEFT JOIN team_members AS tm
                   ON tm.player_id = s.player_id
                       AND tm.left_at IS NULL
         LEFT JOIN teams AS t ON t.team_id = tm.team_id
GROUP BY s.streamer_id, s.nickname, p.name, t.name
ORDER BY total_amount DESC, streamer ASC;
