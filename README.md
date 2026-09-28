<h1 align="center">Apache Cassandra — Practical Guide</h1>

<p align="center">
  <img alt="Cassandra 5.0" src="https://img.shields.io/badge/Cassandra-5.0-1287B1?logo=apachecassandra&logoColor=white">
  <img alt=".NET 10" src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white">
  <img alt="Driver" src="https://img.shields.io/badge/CassandraCSharpDriver-3.23.0-555">
  <img alt="Docker Compose" src="https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white">
</p>

One concept per file. Every file: **Problem Summary → Mermaid Diagram → Concrete Example → Reference** (official docs). Style rules: [CONTRIBUTING.md](CONTRIBUTING.md).

```mermaid
flowchart LR
    A[01 Basics] --> B[02 Architecture] --> C[03 Storage] --> D[04 Read / Write]
    D --> E[05 Cost] --> F[06 Modeling] --> G[07 CQL + .NET]
    G --> H[08 Best Practices] --> I[09 Anti-Patterns]
```

## 01 · Basics
| File | Topic |
|---|---|
| [01](docs/01-basics/01-what-is-cassandra.md) | What is Cassandra |
| [02](docs/01-basics/02-when-to-use.md) | When to use / not use |
| [03](docs/01-basics/03-data-hierarchy.md) | Cluster → Keyspace → Table → Partition → Row → Cell |

## 02 · Architecture
| File | Topic |
|---|---|
| [01](docs/02-architecture/01-node.md) | Node |
| [02](docs/02-architecture/02-rack-and-datacenter.md) | Rack & Datacenter |
| [03](docs/02-architecture/03-cluster.md) | Cluster & seeds |
| [04](docs/02-architecture/04-ring-and-tokens.md) | Ring & tokens |
| [05](docs/02-architecture/05-vnodes.md) | Virtual nodes |
| [06](docs/02-architecture/06-coordinator.md) | Coordinator |
| [07](docs/02-architecture/07-gossip-and-failure-detection.md) | Gossip & failure detection |
| [08](docs/02-architecture/08-snitch.md) | Snitch |
| [09](docs/02-architecture/09-replication.md) | Replication |
| [10](docs/02-architecture/10-consistency-levels.md) | Consistency levels |
| [11](docs/02-architecture/11-hinted-handoff.md) | Hinted handoff |
| [12](docs/02-architecture/12-repair.md) | Read repair & anti-entropy repair |

## 03 · Storage Engine
| File | Topic |
|---|---|
| [01](docs/03-storage/01-commitlog.md) | Commit log |
| [02](docs/03-storage/02-memtable.md) | Memtable |
| [03](docs/03-storage/03-sstable.md) | SSTable |
| [04](docs/03-storage/04-bloom-filter-and-caches.md) | Bloom filter & caches |
| [05](docs/03-storage/05-compaction.md) | Compaction (UCS, STCS, LCS, TWCS) |
| [06](docs/03-storage/06-tombstones.md) | Tombstones |

## 04 · Read & Write
| File | Topic |
|---|---|
| [01](docs/04-read-write/01-write-path.md) | Write path |
| [02](docs/04-read-write/02-read-path.md) | Read path |
| [03](docs/04-read-write/03-delete-path.md) | Delete path |
| [04](docs/04-read-write/04-lightweight-transactions.md) | Lightweight transactions (Paxos) |
| [05](docs/04-read-write/05-batches.md) | Batches — overview & decision |
| [06](docs/04-read-write/06-logged-batch.md) | Logged batch (batchlog) |
| [07](docs/04-read-write/07-unlogged-batch.md) | Unlogged batch |
| [08](docs/04-read-write/08-counter-batch.md) | Counter batch |

## 05 · Cost
| File | Topic |
|---|---|
| [01](docs/05-cost/01-operations-cost-ranking.md) | 12 operations ranked cheapest → most expensive |

## 06 · Data Modeling
| File | Topic |
|---|---|
| [01](docs/06-modeling/01-query-first-modeling.md) | Query-first modeling |
| [02](docs/06-modeling/02-primary-key.md) | Partition key vs clustering key |
| [03](docs/06-modeling/03-partition-sizing.md) | Partition sizing & bucketing |
| [04](docs/06-modeling/04-example-chat-app.md) | **Example 1:** Chat app |
| [05](docs/06-modeling/05-example-iot-telemetry.md) | **Example 2:** IoT telemetry |

## 07 · CQL & .NET
| File | Topic |
|---|---|
| [01](docs/07-cql-dotnet/01-cql-ddl.md) | CQL DDL |
| [02](docs/07-cql-dotnet/02-cql-dml.md) | CQL DML |
| [03](docs/07-cql-dotnet/03-dotnet-connect.md) | .NET: Cluster & Session |
| [04](docs/07-cql-dotnet/04-dotnet-prepared-statements.md) | .NET: Prepared statements |
| [05](docs/07-cql-dotnet/05-dotnet-paging.md) | .NET: Paging |
| [06](docs/07-cql-dotnet/06-dotnet-batch-and-lwt.md) | .NET: Batch, counter, LWT |
| [07](docs/07-cql-dotnet/07-dotnet-mapper.md) | .NET: Raw vs Mapper vs LINQ (measured) |

## 08 · Best Practices
| File | Practice |
|---|---|
| [01](docs/08-best-practices/01-one-session-prepared-statements.md) | One session + prepared statements |
| [02](docs/08-best-practices/02-local-quorum-token-aware.md) | `LOCAL_QUORUM` + token-aware routing |
| [03](docs/08-best-practices/03-bounded-partitions.md) | Bounded partitions |
| [04](docs/08-best-practices/04-ttl-with-twcs.md) | TTL + TWCS for time-series |
| [05](docs/08-best-practices/05-unset-instead-of-null.md) | `Unset` instead of `null` |
| [06](docs/08-best-practices/06-idempotent-retries.md) | Idempotent statements + speculative execution |
| [07](docs/08-best-practices/07-monitor-and-repair.md) | Monitor & repair within `gc_grace_seconds` |

## 09 · Anti-Patterns
| File | Anti-pattern |
|---|---|
| [01](docs/09-anti-patterns/01-allow-filtering.md) | `ALLOW FILTERING` / no partition key |
| [02](docs/09-anti-patterns/02-queue-pattern.md) | Cassandra as a queue |
| [03](docs/09-anti-patterns/03-unbounded-partition.md) | Unbounded partition |
| [04](docs/09-anti-patterns/04-hot-partition.md) | Hot partition |
| [05](docs/09-anti-patterns/05-multi-partition-batch.md) | Multi-partition batch for bulk load |
| [06](docs/09-anti-patterns/06-read-before-write.md) | Read-before-write & LWT everywhere |
| [07](docs/09-anti-patterns/07-secondary-index-misuse.md) | Secondary index as primary access path |
| [08](docs/09-anti-patterns/08-relational-thinking.md) | Normalize + join + `COUNT(*)` |

## Run It

```bash
docker compose up -d                                   # Cassandra 5.0, single node, dc1
docker compose exec cassandra cqlsh -f /cql/schema.cql # optional: the app also creates the schema
docker compose exec cassandra cqlsh -f /cql/queries.cql  # Git Bash on Windows: prefix MSYS_NO_PATHCONV=1
dotnet run --project samples/CassandraDemo
dotnet run --project samples/Shortly                    # Web API on :5080 — see samples/Shortly/README.md
```

```text
✅ schema ready
page 1: message #7, message #6, message #5
page 2: message #4, message #3, message #2
page 3: message #1
claim email (sara): True
claim email (omar): False
readings: 10 (newest 88)
alerts today for device-6273: 4
```

```text
cassandra-guide/
├── docs/                   # 57 one-topic files
├── cql/schema.cql          # keyspaces + tables for both examples
├── cql/queries.cql         # queries to try in cqlsh
├── samples/CassandraDemo/  # .NET 10 console app (CassandraCSharpDriver)
├── samples/Shortly/        # .NET 10 Web API: controllers + repositories (INSERT/UPDATE/DELETE/BATCH/TTL)
├── docker-compose.yml
└── ROADMAP.md
```
