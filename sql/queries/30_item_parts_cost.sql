-- 30. Стоимость сборки из базовых составляющих против собственной цены предмета.
-- Берутся предметы со строками в item_recipe, сборка раскрывается до базовых (тех, у кого
-- строк в item_recipe нет, свитки тоже), количества по пути перемножаются; diff = parts_cost - cost.
--
-- Колонки: name, cost, parts_cost, base_parts, diff
-- Порядок: diff по убыванию, при равенстве name по возрастанию
-- Строк:   77

WITH RECURSIVE
    parts AS (SELECT ir.item_id      AS root_id,
                     ir.component_id AS component_id,
                     ir.quantity     AS qty_total
              FROM item_recipe AS ir

              UNION ALL

              SELECT parts.root_id,
                     child.component_id,
                     parts.qty_total * child.quantity
              FROM parts
                       JOIN item_recipe AS child ON child.item_id = parts.component_id),
    base_parts AS (SELECT parts.root_id, parts.component_id, parts.qty_total
                   FROM parts
                   WHERE NOT EXISTS (SELECT 1
                                     FROM item_recipe AS child
                                     WHERE child.item_id = parts.component_id)),
    totals AS (SELECT root_id,
                      SUM(i.cost * bp.qty_total) AS parts_cost,
                      SUM(bp.qty_total)          AS base_parts
               FROM base_parts AS bp
                        JOIN items AS i ON i.item_id = bp.component_id
               GROUP BY root_id)
SELECT i.name,
       i.cost,
       totals.parts_cost,
       totals.base_parts,
       totals.parts_cost - i.cost AS diff
FROM totals
         JOIN items AS i ON i.item_id = totals.root_id
ORDER BY diff DESC, i.name ASC;
