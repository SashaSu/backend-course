-- 26. Нарастающая сумма донатов внутри каждого стрима Вжлинка за 2021 год.
-- Стримы Вжлинка, начавшиеся в 2021 году по UTC, строка на каждый донат; running_total —
-- нарастающая сумма донатов своего стрима по sent_at_sec.
--
-- Колонки: stream_id, title, sent_at_sec, donor, amount, running_total
-- Порядок: stream_id по возрастанию, внутри стрима sent_at_sec по возрастанию
-- Строк:   192

SELECT st.stream_id,
       st.title,
       d.sent_at_sec,
       d.nickname AS donor,
       d.amount,
       SUM(d.amount) OVER (
           PARTITION BY st.stream_id
           ORDER BY d.sent_at_sec, d.donation_id
           ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
       ) AS running_total
FROM streams AS st
         JOIN streamers AS s ON s.streamer_id = st.streamer_id
         JOIN stream_donations AS d ON d.stream_id = st.stream_id
WHERE s.nickname = 'Вжлинк'
  AND st.started_at >= TIMESTAMPTZ '2021-01-01 00:00:00+00'
  AND st.started_at <  TIMESTAMPTZ '2022-01-01 00:00:00+00'
ORDER BY st.stream_id ASC, d.sent_at_sec ASC, d.donation_id ASC;
