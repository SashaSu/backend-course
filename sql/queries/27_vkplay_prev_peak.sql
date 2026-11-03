-- 27. Пик онлайна против пика предыдущего эфира того же стримера.
-- Только площадка 'VK Play Live'; prev_peak — пик предыдущего по started_at эфира того
-- же стримера, delta — их разность; первый эфир каждого стримера не выводится.
--
-- Колонки: streamer, followers, started_at, title, peak_viewers, prev_peak, delta
-- Порядок: followers по убыванию, внутри стримера started_at по возрастанию
-- Строк:   592

WITH vk_streams AS (SELECT s.nickname AS        streamer,
                           s.followers,
                           st.started_at,
                           st.title,
                           st.peak_viewers,
                           LAG(st.peak_viewers) OVER (
               PARTITION BY st.streamer_id
               ORDER BY st.started_at
           ) AS prev_peak, ROW_NUMBER() OVER (
               PARTITION BY st.streamer_id
               ORDER BY st.started_at
           ) AS stream_number
                    FROM streams AS st
                             JOIN streamers AS s ON s.streamer_id = st.streamer_id
                             JOIN platforms AS p ON p.platform_id = s.platform_id
                    WHERE p.name = 'VK Play Live')
SELECT streamer,
       followers,
       started_at,
       title,
       peak_viewers,
       prev_peak,
       peak_viewers - prev_peak AS delta
FROM vk_streams
WHERE stream_number > 1
ORDER BY followers DESC, started_at ASC;
