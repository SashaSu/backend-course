-- 12. Сборные предметы внутри сборных предметов.
-- Пары «предмет — непосредственный компонент», где компонент сам встречается
-- в item_recipe.item_id; каждая пара один раз, component_cost — цена штуки.
--
-- Колонки: item_name, component_name, quantity, component_cost
-- Порядок: item_name по возрастанию, внутри предмета component_name по возрастанию
-- Строк:   36

SELECT parent.name    AS item_name,
       component.name AS component_name,
       ir.quantity,
       component.cost AS component_cost
FROM item_recipe AS ir
         JOIN items AS parent ON parent.item_id = ir.item_id
         JOIN items AS component ON component.item_id = ir.component_id
WHERE EXISTS (SELECT 1
              FROM item_recipe AS nested
              WHERE nested.item_id = component.item_id)
ORDER BY parent.name ASC, component.name ASC;
