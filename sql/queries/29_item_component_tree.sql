-- 29. Полное дерево сборки Guardian Greaves, Bloodthorn, Harpoon и Hurricane Pike.
-- Строка на каждый путь вниз, сам корень не выводится; lvl с 1, qty — quantity последнего
-- шага, qty_total — произведение quantity по пути, path — имена от корня через ' > '.
--
-- Колонки: root_item, lvl, component, qty, qty_total, path
-- Порядок: root_item по возрастанию, затем path по возрастанию
-- Строк:   34

WITH RECURSIVE item_tree AS (SELECT i.item_id                         AS root_id,
                                    i.name                            AS root_item,
                                    component.item_id                 AS component_id,
                                    component.name                    AS component,
                                    ir.quantity                       AS qty,
                                    ir.quantity                       AS qty_total,
                                    1                                 AS lvl,
                                    i.name || ' > ' || component.name AS path
                             FROM items AS i
                                      JOIN item_recipe AS ir ON ir.item_id = i.item_id
                                      JOIN items AS component ON component.item_id = ir.component_id
                             WHERE i.name IN ('Guardian Greaves', 'Bloodthorn', 'Harpoon', 'Hurricane Pike')

                             UNION ALL

                             SELECT tree.root_id,
                                    tree.root_item,
                                    component.item_id,
                                    component.name,
                                    ir.quantity,
                                    tree.qty_total * ir.quantity,
                                    tree.lvl + 1,
                                    tree.path || ' > ' || component.name
                             FROM item_tree AS tree
                                      JOIN item_recipe AS ir ON ir.item_id = tree.component_id
                                      JOIN items AS component ON component.item_id = ir.component_id)
SELECT root_item, lvl, component, qty, qty_total, path
FROM item_tree
ORDER BY root_item ASC, path ASC;
