-- 28. Три самых дорогих предмета в каждой категории лавки.
-- Категория: 'recipe' при is_recipe, иначе 'assembled' при строках в item_recipe.item_id,
-- иначе 'basic'; place 1-3 по cost убыв., при равной цене по name; purchases — строк match_items, иначе 0.
--
-- Колонки: category, place, name, cost, purchases
-- Порядок: category по возрастанию, затем place по возрастанию
-- Строк:   9

WITH categorized AS (SELECT i.item_id,
                            i.name,
                            i.cost,
                            CASE
                                WHEN i.is_recipe THEN 'recipe'
                                WHEN EXISTS (SELECT 1
                                             FROM item_recipe AS ir
                                             WHERE ir.item_id = i.item_id) THEN 'assembled'
                                ELSE 'basic'
                                END                          AS category,
                            COALESCE(purchases.purchases, 0) AS purchases
                     FROM items AS i
                              LEFT JOIN (SELECT item_id, COUNT(*) AS purchases
                                         FROM match_items
                                         GROUP BY item_id) AS purchases ON purchases.item_id = i.item_id),
     ranked AS (SELECT category,
                       name,
                       cost,
                       purchases,
                       ROW_NUMBER() OVER (
               PARTITION BY category
               ORDER BY cost DESC, name ASC
           ) AS place
                FROM categorized)
SELECT category, place, name, cost, purchases
FROM ranked
WHERE place <= 3
ORDER BY category ASC, place ASC;
