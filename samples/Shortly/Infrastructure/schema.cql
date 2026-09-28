CREATE KEYSPACE IF NOT EXISTS shortly
    WITH replication = {'class': 'NetworkTopologyStrategy', 'dc1': 1};

-- Query: slug → link   (redirect, stats, update, delete)
CREATE TABLE IF NOT EXISTS shortly.links_by_slug (
    slug       text PRIMARY KEY,
    url        text,
    user_id    uuid,
    created_at timestamp
);

-- Query: a user's links, newest first   (same data, different partition key)
CREATE TABLE IF NOT EXISTS shortly.links_by_user (
    user_id    uuid,
    created_at timestamp,
    slug       text,
    url        text,
    PRIMARY KEY ((user_id), created_at, slug)
) WITH CLUSTERING ORDER BY (created_at DESC, slug ASC);

-- Query: click count   (a counter table holds only the key + counters, no TTL)
CREATE TABLE IF NOT EXISTS shortly.link_clicks (
    slug   text PRIMARY KEY,
    clicks counter
);
