-- 20. Герои, которых на патче банили чаще, чем брали.
-- Патч — matches.patch_id; в ответе пары с bans строго больше picks, diff = bans - picks.
--
-- Колонки: version, hero, bans, picks, diff
-- Порядок: diff по убыванию, затем hero по возрастанию, затем version по возрастанию
-- Строк:   99

WITH ban_counts AS (SELECT m.patch_id, mb.hero_id, COUNT(*) AS bans
                    FROM match_bans AS mb
                             JOIN matches AS m ON m.match_id = mb.match_id
                    GROUP BY m.patch_id, mb.hero_id),
     pick_counts AS (SELECT m.patch_id, mp.hero_id, COUNT(*) AS picks
                     FROM match_players AS mp
                              JOIN matches AS m ON m.match_id = mp.match_id
                     GROUP BY m.patch_id, mp.hero_id)
SELECT p.version,
       h.name                         AS hero,
       b.bans,
       COALESCE(pk.picks, 0)          AS picks,
       b.bans - COALESCE(pk.picks, 0) AS diff
FROM ban_counts AS b
         LEFT JOIN pick_counts AS pk
                   ON pk.patch_id = b.patch_id AND pk.hero_id = b.hero_id
         JOIN patches AS p ON p.patch_id = b.patch_id
         JOIN heroes AS h ON h.hero_id = b.hero_id
WHERE b.bans > COALESCE(pk.picks, 0)
ORDER BY diff DESC, hero ASC, p.version ASC;
