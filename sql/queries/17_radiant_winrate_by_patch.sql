-- 17. Доля побед светлой стороны по патчам.
-- Патч без единого матча не выводится; radiant_winrate — процент,
-- round(100.0 * radiant_wins / matches, 2).
--
-- Колонки: version, matches, radiant_wins, radiant_winrate
-- Порядок: radiant_winrate (округлённое) по убыванию, при равенстве version по возрастанию
-- Строк:   12

SELECT p.version,
       COUNT(*) AS matches,
       COUNT(*)    FILTER (WHERE m.winner = 'radiant') AS radiant_wins, ROUND(
        100.0 * COUNT(*) FILTER (WHERE m.winner = 'radiant') / COUNT(*),
        2
                                                                        ) AS radiant_winrate
FROM matches AS m
         JOIN patches AS p ON p.patch_id = m.patch_id
GROUP BY p.patch_id, p.version
ORDER BY radiant_winrate DESC, p.version ASC;
