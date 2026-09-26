# .NET — Prepared Statements

[⬅ Back to README](../../README.md)

## Problem Summary

`PrepareAsync` parses the CQL once on the server; later executions send only the statement id + bound values. Faster, safe against CQL injection, and gives the driver the routing key for token-aware routing.

## Mermaid Diagram

```mermaid
sequenceDiagram
    participant App
    participant N as Node
    App->>N: PREPARE "INSERT ... VALUES (?, ?, ?, ?, ?)"  (once, at startup)
    N-->>App: statement id + metadata (partition key indexes)
    loop 16,700 writes/s
        App->>N: EXECUTE id [values]  (no parsing)
    end
```

## Concrete Example

```csharp
// samples/CassandraDemo/IotRepository.cs — prepared once in CreateAsync
_insertReading = await session.PrepareAsync(
    "INSERT INTO iot.readings_by_device_day (device_id, day, ts, temperature, humidity) VALUES (?, ?, ?, ?, ?)");

// per request
object humidityValue = humidity.HasValue ? humidity.Value : Unset.Value;   // Unset → no tombstone
await session.ExecuteAsync(
    _insertReading.Bind(deviceId, day, ts, temperature, humidityValue)
                  .SetIdempotence(true));                                   // safe to retry / speculate
```

| Approach | Server parse | Token-aware | Injection-safe |
|---|:---:|:---:|:---:|
| `SimpleStatement($"... '{id}'")` | every call | ❌ | ❌ |
| `SimpleStatement("... ?", id)` | every call | ⚠️ | ✅ |
| **`PreparedStatement.Bind(id)`** | once | ✅ | ✅ |

## Reference

- [C# driver: Parameterized queries](https://docs.datastax.com/en/developer/csharp-driver/latest/features/parametrized-queries/index.html)

---

[⬅ Back to README](../../README.md)
