using Cassandra;
using ISession = Cassandra.ISession;

namespace Shortly.Repositories;

/// <summary>
/// Counter table. Counters: no INSERT, no TTL, can't share a batch with normal writes,
/// and +1 is NOT idempotent (a retry may count twice).
/// </summary>
public sealed class ClickRepository : IClickRepository
{
    private readonly ISession _session;
    private readonly PreparedStatement _increment;
    private readonly PreparedStatement _select;
    private readonly PreparedStatement _delete;

    private ClickRepository(ISession session, PreparedStatement[] ps)
    {
        _session = session;
        (_increment, _select, _delete) = (ps[0], ps[1], ps[2]);
    }

    public static async Task<ClickRepository> CreateAsync(ISession session)
    {
        var ps = await Task.WhenAll(
            // UPDATE on a counter: the row is created on first +1
            session.PrepareAsync("UPDATE shortly.link_clicks SET clicks = clicks + 1 WHERE slug = ?"),
            session.PrepareAsync("SELECT clicks FROM shortly.link_clicks WHERE slug = ?"),
            session.PrepareAsync("DELETE FROM shortly.link_clicks WHERE slug = ?"));
        return new ClickRepository(session, ps);
    }

    public Task IncrementAsync(string slug) =>
        _session.ExecuteAsync(_increment.Bind(slug)); // idempotence stays false → driver won't retry it

    public async Task<long> GetAsync(string slug)
    {
        var rs = await _session.ExecuteAsync(_select.Bind(slug).SetIdempotence(true));
        return rs.FirstOrDefault()?.GetValue<long>("clicks") ?? 0;
    }

    public Task DeleteAsync(string slug) =>
        _session.ExecuteAsync(_delete.Bind(slug).SetIdempotence(true));
}
