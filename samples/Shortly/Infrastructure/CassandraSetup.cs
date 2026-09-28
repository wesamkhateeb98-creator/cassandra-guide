using Cassandra;
using ISession = Cassandra.ISession;

namespace Shortly.Infrastructure;

/// <summary>
/// One Cluster + one Session per application (thread-safe, long-lived).
/// </summary>
public static class CassandraSetup
{
    public static Task<ISession> ConnectAsync(IConfiguration configuration) =>
        BuildCluster(
            configuration["Cassandra:Host"] ?? "127.0.0.1",
            configuration["Cassandra:LocalDc"] ?? "dc1")
        .ConnectAsync();

    private static ICluster BuildCluster(string host, string localDc) =>
        Cluster.Builder()
            .AddContactPoint(host)
            .WithLoadBalancingPolicy(new DefaultLoadBalancingPolicy(localDc))
            .WithQueryOptions(new QueryOptions().SetConsistencyLevel(ConsistencyLevel.LocalQuorum))
            .Build();

    // Copied next to the binaries by Shortly.csproj (CopyToOutputDirectory).
    private static readonly string SchemaPath = Path.Combine(AppContext.BaseDirectory, "Infrastructure", "schema.cql");

    public static async Task CreateSchemaAsync(ISession session)
    {
        var script = await File.ReadAllTextAsync(SchemaPath);
        var statements = script
            .Split('\n')
            .Where(line => !line.TrimStart().StartsWith("--"))
            .Aggregate(string.Empty, (acc, line) => acc + line + "\n")
            .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (var cql in statements)
            await session.ExecuteAsync(new SimpleStatement(cql));
    }
}
