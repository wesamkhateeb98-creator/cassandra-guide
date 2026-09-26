# Data Hierarchy

[⬅ Back to README](../../README.md)

## Problem Summary

Cluster → Keyspace → Table → **Partition** → Row → Cell. The partition is the unit of distribution and storage; it has no RDBMS equivalent.

## Mermaid Diagram

```mermaid
flowchart TD
    CL[Cluster] --> DC1[Datacenter dc1] --> R1[Rack] --> N1[Node]
    CL --> KS["Keyspace<br/>(replication strategy + RF)"]
    KS --> T[Table]
    T --> P1["Partition<br/>key = conversation_id"]
    P1 --> RW1["Row<br/>clustering = message_id"]
    RW1 --> CE["Cell<br/>value + writetime + TTL"]
```

## Concrete Example

| Level | RDBMS | Example |
|---|---|---|
| Keyspace | Database | `chat` |
| Table | Table | `messages_by_conversation` |
| Partition | — | all messages of conversation `c-42` |
| Row | Row | one message |
| Cell | Column value | `body = 'hi'` |

```sql
SELECT body, WRITETIME(body), TTL(body)
FROM chat.messages_by_conversation
WHERE conversation_id = ? AND bucket = ? LIMIT 1;
-- body | writetime(body)   | ttl(body)
-- hi   | 1790418900000000  | null
```

## Reference

- [CQL Definitions](https://cassandra.apache.org/doc/latest/cassandra/developing/cql/definitions.html)

---

[⬅ Back to README](../../README.md)
