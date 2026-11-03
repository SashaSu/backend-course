-- 10. Все роли героев, которые умеют быть керри.
-- Только герои с ролью 'carry'; у каждого выводятся все его роли, по строке на роль.
--
-- Колонки: name, primary_attr, attack_type, role
-- Порядок: name по возрастанию, внутри героя role по возрастанию
-- Строк:   47

SELECT h.name, h.primary_attr, h.attack_type, hr_all.role
FROM heroes AS h
         JOIN hero_roles AS hr_carry
              ON hr_carry.hero_id = h.hero_id
                  AND hr_carry.role = 'carry'
         JOIN hero_roles AS hr_all ON hr_all.hero_id = h.hero_id
ORDER BY h.name ASC, hr_all.role ASC;
