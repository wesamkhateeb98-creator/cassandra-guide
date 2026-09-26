-- Local single-node schema. Production: 'dc1': 3 (and one entry per DC).

CREATE KEYSPACE IF NOT EXISTS chat
  WITH replication = {'class': 'NetworkTopologyStrategy', 'dc1': 1};

CREATE KEYSPACE IF NOT EXISTS iot
  WITH replication = {'class': 'NetworkTopologyStrategy', 'dc1': 1};

-- ---------- Example 1: Chat ----------

CREATE TABLE IF NOT EXISTS chat.messages_by_conversation (
  conversation_id uuid,
  bucket          int,
  message_id      timeuuid,
  sender_id       uuid,
  body            text,
  PRIMARY KEY ((conversation_id, bucket), message_id)
) WITH CLUSTERING ORDER BY (message_id DESC);

CREATE TABLE IF NOT EXISTS chat.conversations_by_user (
  user_id              uuid,
  conversation_id      uuid,
  title                text,
  last_message_at      timestamp,
  last_message_preview text,
  PRIMARY KEY (user_id, conversation_id)
);

CREATE TABLE IF NOT EXISTS chat.unread_by_user (
  user_id         uuid,
  conversation_id uuid,
  unread          counter,
  PRIMARY KEY (user_id, conversation_id)
);

CREATE TABLE IF NOT EXISTS chat.users_by_email (
  email   text PRIMARY KEY,
  user_id uuid
);

-- ---------- Example 2: IoT ----------

CREATE TABLE IF NOT EXISTS iot.readings_by_device_day (
  device_id   text,
  day         date,
  ts          timestamp,
  temperature double,
  humidity    double,
  PRIMARY KEY ((device_id, day), ts)
) WITH CLUSTERING ORDER BY (ts DESC)
  AND default_time_to_live = 2592000
  AND compaction = {'class': 'TimeWindowCompactionStrategy',
                    'compaction_window_unit': 'DAYS',
                    'compaction_window_size': 1};

CREATE TABLE IF NOT EXISTS iot.latest_reading_by_device (
  device_id   text PRIMARY KEY,
  ts          timestamp,
  temperature double,
  humidity    double
);

CREATE TABLE IF NOT EXISTS iot.alerts_by_day (
  day         date,
  bucket      int,
  ts          timestamp,
  device_id   text,
  temperature double,
  PRIMARY KEY ((day, bucket), ts, device_id)
) WITH CLUSTERING ORDER BY (ts DESC, device_id ASC);
