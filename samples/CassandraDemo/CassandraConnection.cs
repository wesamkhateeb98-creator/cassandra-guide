using Cassandra;

namespace CassandraDemo;

/// <summary>
/// One Cluster + one Session per application (thread-safe, long-lived).
/// </summary>
public static class CassandraConnection
{
    public static ICluster BuildCluster(string contactPoint = "127.0.0.1", string localDc = "dc1") =>
        Cluster.Builder()
            .AddContactPoint(contactPoint)
            .WithPort(9042)
            // Token-aware + DC-aware: send each query straight to a local replica.
            .WithLoadBalancingPolicy(new DefaultLoadBalancingPolicy(localDc))
            .WithQueryOptions(new QueryOptions()
                .SetConsistencyLevel(ConsistencyLevel.LocalQuorum)
                .SetSerialConsistencyLevel(ConsistencyLevel.LocalSerial)
                .SetPageSize(500))
            // Retry slow reads on another replica after 50 ms (idempotent statements only).
            .WithSpeculativeExecutionPolicy(new ConstantSpeculativeExecutionPolicy(50, 1))
            .WithSocketOptions(new SocketOptions().SetReadTimeoutMillis(12_000))
            .Build();

    public static async Task CreateSchemaAsync(ISession session, string path)
    {
        var script = await File.ReadAllTextAsync(path);
        var statements = script
            .Split('\n')
            .Where(line => !line.TrimStart().StartsWith("--"))
            .Aggregate(string.Empty, (acc, line) => acc + line + "\n")
            .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (var cql in statements)
            await session.ExecuteAsync(new SimpleStatement(cql));
    }
}
