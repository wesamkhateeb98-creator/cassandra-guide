-- Run: docker compose exec cassandra cqlsh -f /cql/queries.cql

-- ---------- Chat ----------
INSERT INTO chat.messages_by_conversation (conversation_id, bucket, message_id, sender_id, body)
VALUES (11111111-1111-1111-1111-111111111111, 202609, now(), 22222222-2222-2222-2222-222222222222, 'hello');

INSERT INTO chat.messages_by_conversation (conversation_id, bucket, message_id, sender_id, body)
VALUES (11111111-1111-1111-1111-111111111111, 202609, now(), 33333333-3333-3333-3333-333333333333, 'hi 👋');

-- Q1: last 50 messages (single partition, clustering order DESC)
SELECT toTimestamp(message_id) AS sent_at, sender_id, body
FROM chat.messages_by_conversation
WHERE conversation_id = 11111111-1111-1111-1111-111111111111 AND bucket = 202609
LIMIT 50;

-- Counter
UPDATE chat.unread_by_user SET unread = unread + 1
WHERE user_id = 33333333-3333-3333-3333-333333333333
  AND conversation_id = 11111111-1111-1111-1111-111111111111;

-- LWT: unique email
INSERT INTO chat.users_by_email (email, user_id)
VALUES ('sara@example.com', 22222222-2222-2222-2222-222222222222) IF NOT EXISTS;
INSERT INTO chat.users_by_email (email, user_id)
VALUES ('sara@example.com', 33333333-3333-3333-3333-333333333333) IF NOT EXISTS;  -- [applied] = False

-- ---------- IoT ----------
INSERT INTO iot.readings_by_device_day (device_id, day, ts, temperature, humidity)
VALUES ('device-17', '2026-09-26', '2026-09-26 10:15:00+0000', 21.4, 40);

INSERT INTO iot.latest_reading_by_device (device_id, ts, temperature, humidity)
VALUES ('device-17', '2026-09-26 10:15:00+0000', 21.4, 40);

-- Q1: range on clustering column
SELECT ts, temperature FROM iot.readings_by_device_day
WHERE device_id = 'device-17' AND day = '2026-09-26'
  AND ts >= '2026-09-26 10:00:00+0000' AND ts < '2026-09-26 11:00:00+0000';

-- Metadata per cell
SELECT temperature, WRITETIME(temperature), TTL(temperature)
FROM iot.readings_by_device_day
WHERE device_id = 'device-17' AND day = '2026-09-26' LIMIT 1;

-- Which token / node owns the partition
SELECT token(device_id, day), device_id, day FROM iot.readings_by_device_day LIMIT 3;
